using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Utiliteez.RenderEngine.Interfaces;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Utiliteez.RenderEngine;

public unsafe record CommandManager (
    Vk Vk,
    IDeviceManager DeviceManager,
    ISwapChainManager SwapChainManager,
    IResourceManager ResourceManager,
    IPipelineManager PipelineManager
    ) : ICommandManager
{
    private int _frameIndex = 0;
    private bool _isInitialized = false;
    public void Initialize()
    {
        if (_isInitialized)
            return;
        DeviceManager.CreateCommandBuffers(SwapChainManager.ImageCount);
        _isInitialized = true;
    }
    
    public void RenderFrame(
        VulkanBuffer vertexBuffer,
        VulkanBuffer indexBuffer,
        VulkanBuffer IndirectCommandBuffer,
        VulkanBuffer InstanceDataBuffer,
        uint drawCount)
    {
        _frameIndex = (_frameIndex + 1) % SwapChainManager.ImageCount;
        
        // Wait for the in-flight fence to ensure the previous frame is complete
        Vk.WaitForFences(DeviceManager.LogicalDevice, 1, in SwapChainManager.InFlightFences[_frameIndex], true, ulong.MaxValue);
        Vk.ResetFences(DeviceManager.LogicalDevice, 1, in SwapChainManager.InFlightFences[_frameIndex]);
        
        Vk.ResetCommandBuffer(
            DeviceManager.CommandBuffers[_frameIndex],
            CommandBufferResetFlags.None
        );
        
        ResourceManager.UpdateUniformBuffer();

        // Acquire the next image from the swapchain
        uint imageIndex;
        var result = SwapChainManager.KhrSwapChain.AcquireNextImage(DeviceManager.LogicalDevice, SwapChainManager.SwapChainKhr, ulong.MaxValue, SwapChainManager.ImageAvailableSemaphores[_frameIndex], default,
            &imageIndex);
        if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire swapchain image!");
        }


        RenderingAttachmentInfo renderAttachmentInfo = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = SwapChainManager.SwapChainImageViews[imageIndex], // Was misnamed? SwapChainViews
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue
            {
                Color = new ClearColorValue { Float32_0 = 1f, Float32_1 = 1f, Float32_2 = 1f, Float32_3 = 1 },
            },
        };
        // DEPTH attachment info
        
        var depthAttach = new RenderingAttachmentInfo {
            SType          = StructureType.RenderingAttachmentInfo,
            ImageView      = SwapChainManager.DepthImageView,
            ImageLayout    = ImageLayout.DepthStencilAttachmentOptimal,
            LoadOp         = AttachmentLoadOp.Clear,
            StoreOp        = AttachmentStoreOp.DontCare,
            ClearValue     = new ClearValue { DepthStencil = new ClearDepthStencilValue { Depth = 1.0f, Stencil = 0 } }
        };
        
        RenderingInfo renderInfo = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D
            {
                Offset = new Offset2D { X = 0, Y = 0 },
                Extent = SwapChainManager.SwapChainExtent,
            },
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &renderAttachmentInfo,
            PDepthAttachment = &depthAttach,
        };
        
        // Record dynamic rendering commands
        Vk.BeginCommandBuffer(DeviceManager.CommandBuffers[_frameIndex],
            new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo });

        ImageMemoryBarrier memoryBarrierBegin = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.ColorAttachmentOptimal,
            Image = SwapChainManager.SwapChainImages[imageIndex],
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        Vk.CmdPipelineBarrier(DeviceManager.CommandBuffers[_frameIndex], PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.ColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &memoryBarrierBegin);

        
// Use the function
        Vk.CmdBeginRendering(DeviceManager.CommandBuffers[_frameIndex], &renderInfo);
        Vk!.CmdBindPipeline(DeviceManager.CommandBuffers[_frameIndex], PipelineBindPoint.Graphics, PipelineManager.GraphicsPipeline);

        fixed (DescriptorSet* descriptorSet = &PipelineManager.DescriptorSet)
        {
            Vk.CmdBindDescriptorSets(DeviceManager.CommandBuffers[_frameIndex], PipelineBindPoint.Graphics, PipelineManager.GraphicsPipelineLayout, 0, 1,
                descriptorSet, 0, null);
    
        }
        
        fixed (Buffer*    pBuffers = &vertexBuffer.Buffer)
        fixed (ulong*     pOffsets = stackalloc ulong[1] { 0 })
        {
            Vk.CmdBindVertexBuffers(
                DeviceManager.CommandBuffers[_frameIndex],
                0,               // first binding
                1,               // binding count
                pBuffers,        // pointer to your 1-element array of Buffer
                pOffsets         // pointer to your 1-element array of offsets
            );
        }
        
        Vk.CmdBindIndexBuffer(DeviceManager.CommandBuffers[_frameIndex], indexBuffer.Buffer, 0, IndexType.Uint32);
        
        Vk.CmdDrawIndexedIndirect(DeviceManager.CommandBuffers[_frameIndex], IndirectCommandBuffer.Buffer, 0, drawCount, (uint)Marshal.SizeOf<DrawIndexedIndirectCommand>());
        
        Vk.CmdEndRendering(DeviceManager.CommandBuffers[_frameIndex]);

        ImageMemoryBarrier memoryBarrierEnd = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
            OldLayout = ImageLayout.ColorAttachmentOptimal,
            NewLayout = ImageLayout.PresentSrcKhr,
            Image = SwapChainManager.SwapChainImages[imageIndex],
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        Vk.CmdPipelineBarrier(DeviceManager.CommandBuffers[_frameIndex], PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, &memoryBarrierEnd);

        Vk.EndCommandBuffer(DeviceManager.CommandBuffers[_frameIndex]);

        var stageMask = PipelineStageFlags.ColorAttachmentOutputBit;

        fixed(Semaphore* renderFinishedSemaphore = &SwapChainManager.RenderFinishedSemaphores[_frameIndex])
        fixed(Semaphore* imageAvailableSemaphore = &SwapChainManager.ImageAvailableSemaphores[_frameIndex])
        fixed(CommandBuffer* commandBuffer = &DeviceManager.CommandBuffers[_frameIndex])
        fixed(SwapchainKHR* SwapChainKhr = &SwapChainManager.SwapChainKhr)
        {
            // Submit the command buffer
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = imageAvailableSemaphore,
                PWaitDstStageMask = &stageMask,
                CommandBufferCount = 1,
                PCommandBuffers = commandBuffer,
                SignalSemaphoreCount = 1,
                PSignalSemaphores = renderFinishedSemaphore,
            };

            Vk.QueueSubmit(DeviceManager.GraphicsQueue, 1, in submitInfo, SwapChainManager.InFlightFences[_frameIndex]);

            // Present the swapchain image
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = renderFinishedSemaphore,
                SwapchainCount = 1,
                PSwapchains = SwapChainKhr,
                PImageIndices = &imageIndex,
            }; 
            SwapChainManager.KhrSwapChain.QueuePresent(DeviceManager.PresentQueue, in presentInfo);
        }
    }
}