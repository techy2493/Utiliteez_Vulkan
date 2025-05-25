using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Utiliteez.RenderEngine.Interfaces;
using Utiliteez.RenderEngine.shaders;
using Utiliteez.RenderEngine.Structs;

namespace Utiliteez.RenderEngine;

public unsafe record PipelineManager(
    Vk Vk,
    IDeviceManager DeviceManager,
    IResourceManager ResourceManager,
    ISwapChainManager SwapChainManager) : IPipelineManager
{
    public PipelineLayout GraphicsPipelineLayout
    {
        get => _graphicsPipelineLayout;
        private set => _graphicsPipelineLayout = value;
    }

    public Pipeline GraphicsPipeline
    {
        get => _graphicsPipeline;
        private set => _graphicsPipeline = value;
    }

    public DescriptorSetLayout descriptorSetLayout;
    public DescriptorPool descriptorPool;
    private DescriptorSet _descriptorSet;
    private readonly Vk _vk = Vk;
    private readonly IDeviceManager _deviceManager = DeviceManager;
    private readonly IResourceManager _resourceManager = ResourceManager;
    private readonly ISwapChainManager _swapChainManager = SwapChainManager;
    private PipelineLayout _graphicsPipelineLayout;
    private Pipeline _graphicsPipeline;

    public ref readonly DescriptorSet DescriptorSet => ref _descriptorSet;

    public Vk Vk
    {
        get => _vk;
        init => _vk = value;
    }

    public IDeviceManager DeviceManager
    {
        get => _deviceManager;
        init => _deviceManager = value;
    }

    public IResourceManager ResourceManager
    {
        get => _resourceManager;
        init => _resourceManager = value;
    }

    public ISwapChainManager SwapChainManager
    {
        get => _swapChainManager;
        init => _swapChainManager = value;
    }

    public void Initialize()
    {
        CreateDescriptorSetLayout();
        CreateDescriptorPool();
        AllocateDescriptorSet();       // just allocates once
        WriteDescriptorsForUBO();      // binding 0 → your uniform buffer
        WriteDescriptorsForMaterialBuffer();
        WriteDescriptorsForInstanceDataBuffer();
        WriteDescriptorForAtlas();// binding 1 → your initial material buffer
        ResourceManager.SetAtlasDescriptorSet(_descriptorSet);
        CreateGraphicsPipeline();
    }
    public void CreateDescriptorSetLayout()
{
    DescriptorSetLayoutBinding uboLayoutBinding = new()
    {
        Binding = 0,
        DescriptorType = DescriptorType.UniformBuffer,
        DescriptorCount = 1,
        StageFlags = ShaderStageFlags.VertexBit,
        PImmutableSamplers = null
    };
    

    // binding 1 = our runtime StorageBuffer:
    DescriptorSetLayoutBinding matBufferBinding = new()
    {
        Binding = 1,
        DescriptorType = DescriptorType.StorageBuffer,
        DescriptorCount = 1,
        StageFlags = ShaderStageFlags.FragmentBit
    };
    
    var samplerBinding = new DescriptorSetLayoutBinding {
        Binding         = 2,
        DescriptorType  = DescriptorType.CombinedImageSampler,
        DescriptorCount = 1,
        StageFlags      = ShaderStageFlags.FragmentBit,
        PImmutableSamplers = null
    };
    
    
    
    DescriptorSetLayoutBinding InstanceDataBinding = new()
    {
        Binding = 3,
        DescriptorType = DescriptorType.StorageBuffer,
        DescriptorCount = 1,
        StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
        PImmutableSamplers = null
    };

        
    var bindings = new[] { uboLayoutBinding, matBufferBinding, samplerBinding, InstanceDataBinding };
    fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
    {
        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindings.Length,
            PBindings = bindingsPtr
        };

        if (Vk.CreateDescriptorSetLayout(DeviceManager.LogicalDevice, &layoutInfo, null, out descriptorSetLayout) != Result.Success)
        {
            throw new Exception("failed to create descriptor set layout!");
        }
    }
}

    internal void CreateDescriptorPool()
    {
        DescriptorPoolSize[] poolSizes = new []
            {
                new DescriptorPoolSize
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = 1
                },
                new DescriptorPoolSize
                {
                    Type = DescriptorType.StorageBuffer,
                    DescriptorCount = 2
                },
                new DescriptorPoolSize {
                    Type            = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1
                },
            };
        

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = 1
            };

            fixed (DescriptorPool* pDescriptorPool = &descriptorPool)
            {
                if (Vk.CreateDescriptorPool(DeviceManager.LogicalDevice, &poolInfo, null, pDescriptorPool) != Result.Success)
                {
                    throw new Exception("failed to create descriptor pool!");
                }
            }
        }
    }
    
    /// <summary>
    /// Allocate the descriptor set (binding layout must already exist, and pool must be created).
    /// After this call, _descriptorSet is valid and you can call your WriteDescriptorsFor… methods.
    /// </summary>
    public void AllocateDescriptorSet()
    {
        // Point at our single DescriptorSetLayout
        fixed (DescriptorSetLayout* pLayout = &descriptorSetLayout)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType              = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool     = descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts        = pLayout
            };

            // Allocate one DescriptorSet into our backing field
            fixed (DescriptorSet* pDescSet = &_descriptorSet)
            {
                if (Vk.AllocateDescriptorSets(
                        DeviceManager.LogicalDevice,
                        &allocInfo,
                        pDescSet
                    ) != Result.Success)
                {
                    throw new Exception("Failed to allocate descriptor set!");
                }
            }
        }
    }
    
    
    /// <summary>
    /// Update binding 0 (std140 UBO) with the current uniform buffer.
    /// </summary>
    public void WriteDescriptorsForUBO()
    {
        // Describe the uniform buffer (matrices)
        var uboSize = (ulong) Marshal.SizeOf<CameraUniformBufferObject>();
        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = ResourceManager.UniformBuffer.Buffer,
            Offset = 0,
            Range  = uboSize
        };

        // Build the WriteDescriptorSet for binding 0
        var write = new WriteDescriptorSet
        {
            SType           = StructureType.WriteDescriptorSet,
            DstSet          = _descriptorSet,
            DstBinding      = 0,
            DstArrayElement = 0,
            DescriptorType  = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo     = &bufferInfo
        };

        // Issue the update
        Vk.UpdateDescriptorSets(
            DeviceManager.LogicalDevice,
            1,
            &write,
            0,
            null
        );
    }

    /// <summary>
    /// Update binding 1 (std430 SSBO) with the current material buffer.
    /// Call this any time ResourceManager.MaterialBuffer or its size changes.
    /// </summary>
    ///
    public void WriteDescriptorsForMaterialBuffer()
    {
        var matsInfo = new DescriptorBufferInfo
        {
            Buffer = ResourceManager.MaterialBuffer.Buffer,
            Offset = 0,
            Range  = ResourceManager.MaterialBufferSize
        };

        var write = new WriteDescriptorSet
        {
            SType           = StructureType.WriteDescriptorSet,
            DstSet          = _descriptorSet,
            DstBinding      = 1,
            DstArrayElement = 0,
            DescriptorType  = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            PBufferInfo     = &matsInfo
        };

        Vk.UpdateDescriptorSets(
            DeviceManager.LogicalDevice,
            1,
            &write,
            0,
            null
        );
    }
    
    
    public void WriteDescriptorsForInstanceDataBuffer()
    {
        var instanceInfo = new DescriptorBufferInfo
        {
            Buffer = ResourceManager.InstanceDataBuffer.Buffer,
            Offset = 0,
            Range  = ResourceManager.InstanceDataBufferSize
        };

        var write = new WriteDescriptorSet
        {
            SType           = StructureType.WriteDescriptorSet,
            DstSet          = _descriptorSet,
            DstBinding      = 3,
            DstArrayElement = 0,
            DescriptorType  = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            PBufferInfo     = &instanceInfo
        };

        Vk.UpdateDescriptorSets(
            DeviceManager.LogicalDevice,
            1,
            &write,
            0,
            null
        );
    }
    
    public void WriteDescriptorForAtlas()
    {
        var imageInfo = new DescriptorImageInfo {
            Sampler     = ResourceManager.AtlasSampler,
            ImageView   = ResourceManager.AtlasImageView,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal
        };

        var write = new WriteDescriptorSet {
            SType           = StructureType.WriteDescriptorSet,
            DstSet          = _descriptorSet,
            DstBinding      = 2,  // must match shader’s binding=2
            DescriptorType  = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo      = &imageInfo
        };

        Vk.UpdateDescriptorSets(DeviceManager.LogicalDevice, 1, &write, 0, null);
    }

internal unsafe void CreateGraphicsPipeline()
    {
        using (var vertShader = new Shader("shaders/vert.spv", ShaderStageFlags.VertexBit, Vk, DeviceManager.LogicalDevice))
        using (var fragShader = new Shader("shaders/frag.spv", ShaderStageFlags.FragmentBit, Vk, DeviceManager.LogicalDevice))
        {
            var bindingDescription = new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)sizeof(Vertex),
                InputRate = VertexInputRate.Vertex
            };

            var attributeDescriptions = stackalloc VertexInputAttributeDescription[3];
            attributeDescriptions[0] = new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position))
            };
            attributeDescriptions[1] = new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal))
            };
            attributeDescriptions[2] = new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Uv))
            };
            
            
            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDescription,
                VertexAttributeDescriptionCount = 3,
                PVertexAttributeDescriptions = attributeDescriptions
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = SwapChainManager.SwapChainExtent.Width,
                Height = SwapChainManager.SwapChainExtent.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = SwapChainManager.SwapChainExtent,
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor,
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.None,//CullModeFlags.BackBit,
                FrontFace = FrontFace.Clockwise,//FrontFace.Clockwise,
                DepthBiasEnable = false,
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                                 ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;

            fixed(DescriptorSetLayout* descriptorSetLayout = &this.descriptorSetLayout)
            {
                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 1,
                    PSetLayouts = descriptorSetLayout,
                    PushConstantRangeCount = 0
                };

                if (Vk!.CreatePipelineLayout(DeviceManager.LogicalDevice, in pipelineLayoutInfo, null, out _graphicsPipelineLayout) != Result.Success)
                {
                    throw new Exception("failed to create pipeline layout!");
                }
            }
            

            var shaderStages = stackalloc[]
            {
                vertShader.GetShaderStageCreateInfo(),
                fragShader.GetShaderStageCreateInfo()
            };
            

            var format = SwapChainManager.SwapChainImageFormat;
            PipelineRenderingCreateInfo renderingCreateInfo = new()
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = 1,
                PColorAttachmentFormats = &format,
                DepthAttachmentFormat = Format.D32Sfloat,
                StencilAttachmentFormat = Format.Undefined,
            };
            
            var depthStencil = new PipelineDepthStencilStateCreateInfo {
                SType            = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable  = true,
                DepthWriteEnable = true,
                StencilTestEnable = false,
                DepthCompareOp   = CompareOp.LessOrEqual,
            };
            
            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &renderingCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PColorBlendState = &colorBlending,
                PDepthStencilState = &depthStencil,
                Layout = GraphicsPipelineLayout,
                Subpass = 0,
                BasePipelineHandle = default
                
            };

            if (Vk!.CreateGraphicsPipelines(DeviceManager.LogicalDevice, default, 1, in pipelineInfo, null, out _graphicsPipeline) !=
                Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }
    }
}