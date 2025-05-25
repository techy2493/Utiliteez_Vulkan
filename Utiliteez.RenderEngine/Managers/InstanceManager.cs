using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.Tools.Logging;


namespace Utiliteez.RenderEngine;

public unsafe record InstanceManager(
    Vk Vk,
    IWindowManager Window
) : IInstanceManager
{
    
    public Instance Instance { get; private set; }
    public ExtDebugUtils? DebugUtils { get; private set; }
    public DebugUtilsMessengerEXT DebugMessenger { get; private set; }

    public string[] DeviceExtensions { get; } =
    [
        KhrSwapchain.ExtensionName,
        KhrDynamicRendering.ExtensionName,
        "VK_KHR_synchronization2"
    ];

    public bool EnableValidationLayers
    {
        get
        {
#if DEBUG
            return true;
#else
            return false
#endif
        }
    }


    public string[] ValidationLayers { get; } =
    [
        "VK_LAYER_KHRONOS_validation"
    ];

    public bool CheckValidationLayerSupport()
    {
        unsafe
        {
            uint layerCount = 0;
            Vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                Vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
            }

            var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName))
                .ToHashSet();

            return ValidationLayers.All(availableLayerNames.Contains);
        }
    }

    public string[] GetRequiredInstanceExtensions()
    {
        var glfwExtensions = Window.Window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
        {
            if (!CheckValidationLayerSupport())
            {
                throw new Exception("validation layers requested, but not available!");
            }

            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }


        return extensions;
    }

    public bool CheckDeviceExtensionsSupport(PhysicalDevice device)
    {
        uint extensionsCount = 0;
        Vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionsCount, null);

        var availableExtensions = new ExtensionProperties[extensionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            Vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionsCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions
            .Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return DeviceExtensions.All(availableExtensionNames.Contains);
    }

    public static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData
    )
    {
        Console.WriteLine(
            "validation layer: " +
            Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage)
        );
        return Vk.False;
    }

    public static DebugUtilsMessengerCallbackFunctionEXT DebugDelegate = DebugCallback;
    public static void PopulateDebugMessengerCreateInfo(
        ref DebugUtilsMessengerCreateInfoEXT createInfo
    )
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt
                                     | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                                     | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                                 | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
                                 | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = DebugDelegate;
    }

    public void SetupDebugMessenger()
    {
        if (!EnableValidationLayers)
            return;

        // TryGetInstanceExtension equivalent to CreateDebugUtilsMessengerEXT
        if (!Vk.TryGetInstanceExtension(Instance, out ExtDebugUtils extUtils))
            return;

        DebugUtils = extUtils;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (DebugUtils!.CreateDebugUtilsMessenger(
                Instance, in createInfo, null, out var messenger
            ) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }

        DebugMessenger = messenger;
    }


    public void Initialize()
    {
        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Utiliteez"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredInstanceExtensions();
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        ;

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(ValidationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (Vk.CreateInstance(in createInfo, null, out var instance) != Result.Success)
        {
            var ex = new Exception("failed to create instance!");
            Logger.Log.Fatal(ex);
            throw ex;
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

        //Cleanup ??
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        Instance = instance;
        SetupDebugMessenger();
    }
}