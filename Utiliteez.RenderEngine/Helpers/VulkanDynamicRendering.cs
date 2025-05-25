using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Utiliteez.RenderEngine;


public static unsafe class VulkanDynamicRendering
{
    public delegate void CmdBeginRenderingKHRDelegate(CommandBuffer commandBuffer, RenderingInfoKHR* renderingInfo);

    public static CmdBeginRenderingKHRDelegate LoadCmdBeginRenderingKHR(Vk vk, Device device)
    {
        var functionPointer = vk.GetDeviceProcAddr(device, "vkCmdBeginRenderingKHR");

        if (functionPointer == IntPtr.Zero)
        {
            throw new Exception("Failed to load  .");
        }

        return (CmdBeginRenderingKHRDelegate)Marshal.GetDelegateForFunctionPointer(
            (IntPtr)functionPointer, typeof(CmdBeginRenderingKHRDelegate));
    }

    public delegate void CmdEndRenderingKHRDelegate(CommandBuffer commandBuffer);

    public static CmdEndRenderingKHRDelegate LoadCmdEndRenderingKHR(Vk vk, Device device)
    {
        var functionPointer = vk.GetDeviceProcAddr(device, "vkCmdEndRenderingKHR");

        if (functionPointer == IntPtr.Zero)
        {
            throw new Exception("Failed to load vkCmdEndRenderingKHR.");
        }

        return (CmdEndRenderingKHRDelegate)Marshal.GetDelegateForFunctionPointer(
            (IntPtr)functionPointer, typeof(CmdEndRenderingKHRDelegate));
    }
}