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
    public void RenderFrame(
        VulkanBuffer vertexBuffer,
        VulkanBuffer indexBuffer,
        uint indexCount)
    {
        // Wait for the in-flight fence to ensure the previous frame is complete
        Vk.WaitForFences(DeviceManager.LogicalDevice, 1, in SwapChainManager.InFlightFences, true, ulong.MaxValue);
        Vk.ResetFences(DeviceManager.LogicalDevice, 1, in SwapChainManager.InFlightFences);
        
        Vk.ResetCommandBuffer(
            DeviceManager.CommandBuffer,
            CommandBufferResetFlags.None
        );
        
        ResourceManager.UpdateUniformBuffer();

        // Acquire the next image from the swapchain
        uint imageIndex;
        var result = SwapChainManager.KhrSwapChain.AcquireNextImage(DeviceManager.LogicalDevice, SwapChainManager.SwapChainKhr, ulong.MaxValue, SwapChainManager.ImageAvailableSemaphore, default,
            &imageIndex);
        if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire swapchain image!");
        }


        RenderingAttachmentInfo renderAttachmentInfo = new()
        {
            SType = StructureType.RenderingAttachmentInfoKhr,
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
        
        RenderingInfoKHR renderInfo = new()
        {
            SType = StructureType.RenderingInfoKhr,
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
        
        var beginRenderingKHR = VulkanDynamicRendering.LoadCmdBeginRenderingKHR(Vk, DeviceManager.LogicalDevice);
        var endRenderingKHR = VulkanDynamicRendering.LoadCmdEndRenderingKHR(Vk, DeviceManager.LogicalDevice);
        // Record dynamic rendering commands
        Vk.BeginCommandBuffer(DeviceManager.CommandBuffer,
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

        Vk.CmdPipelineBarrier(DeviceManager.CommandBuffer, PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.ColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &memoryBarrierBegin);

        
// Use the function
        beginRenderingKHR(DeviceManager.CommandBuffer, &renderInfo);
        Vk!.CmdBindPipeline(DeviceManager.CommandBuffer, PipelineBindPoint.Graphics, PipelineManager.GraphicsPipeline);

        fixed (DescriptorSet* descriptorSet = &PipelineManager.DescriptorSet)
        {
            Vk.CmdBindDescriptorSets(DeviceManager.CommandBuffer, PipelineBindPoint.Graphics, PipelineManager.GraphicsPipelineLayout, 0, 1,
                descriptorSet, 0, null);
    
        }
        
        // vk.CmdDraw(commandBuffer, 3, 1, 0, 0);
        // vk.CmdDraw(commandBuffer, 3, 2, 3, 1);
        
        
        fixed (Buffer*    pBuffers = &vertexBuffer.Buffer)
        fixed (ulong*     pOffsets = stackalloc ulong[1] { 0 })
        {
            Vk.CmdBindVertexBuffers(
                DeviceManager.CommandBuffer,
                0,               // first binding
                1,               // binding count
                pBuffers,        // pointer to your 1-element array of Buffer
                pOffsets         // pointer to your 1-element array of offsets
            );
        }
        
        Vk.CmdBindIndexBuffer(DeviceManager.CommandBuffer, indexBuffer.Buffer, 0, IndexType.Uint32);
        
        Vk.CmdDrawIndexed(DeviceManager.CommandBuffer, indexCount, 1, 0, 0, 0);
        
        endRenderingKHR(DeviceManager.CommandBuffer);

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

        Vk.CmdPipelineBarrier(DeviceManager.CommandBuffer, PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, &memoryBarrierEnd);

        Vk.EndCommandBuffer(DeviceManager.CommandBuffer);

        var stageMask = PipelineStageFlags.ColorAttachmentOutputBit;

        fixed(Semaphore* renderFinishedSemaphore = &SwapChainManager.RenderFinishedSemaphores)
        fixed(Semaphore* imageAvailableSemaphore = &SwapChainManager.ImageAvailableSemaphore)
        fixed(CommandBuffer* commandBuffer = &DeviceManager.CommandBuffer)
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

            Vk.QueueSubmit(DeviceManager.GraphicsQueue, 1, in submitInfo, SwapChainManager.InFlightFences);

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