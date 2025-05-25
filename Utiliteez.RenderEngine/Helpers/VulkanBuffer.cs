using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Utiliteez.RenderEngine;

public unsafe class VulkanBuffer
{
    public Buffer Buffer;
    public DeviceMemory Memory;

    public static VulkanBuffer CreateBuffer(Vk vk, Device device, PhysicalDevice physicalDevice, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        VulkanBuffer buffer = new VulkanBuffer();

        BufferCreateInfo bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer.Buffer)
        {
            vk.CreateBuffer(device, &bufferInfo, null, bufferPtr);
        }
        
        MemoryRequirements memRequirements;
        vk.GetBufferMemoryRequirements(device, buffer.Buffer, &memRequirements);

        MemoryAllocateInfo allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(vk, physicalDevice, memRequirements.MemoryTypeBits, properties),
        };


        fixed (DeviceMemory* memoryPtr = &buffer.Memory)
        {
            vk.AllocateMemory(device, &allocInfo, null, memoryPtr);
        }
        
        vk.BindBufferMemory(device, buffer.Buffer, buffer.Memory, 0);

        return buffer;
    }

    public static uint FindMemoryType(Vk vk, PhysicalDevice physicalDevice, uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memProperties;
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);

        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }
}