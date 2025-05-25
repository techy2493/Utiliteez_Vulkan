using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Utiliteez.RenderEngine.shaders;

public unsafe class Shader: IDisposable
{
    private readonly string _filename;
    private readonly ShaderStageFlags _type;
    private readonly Vk _vk;
    private readonly Device _device;
    private ShaderModule _module;
    private PipelineShaderStageCreateInfo _pipelineShaderStageCreateInfo;

    internal Shader(string filename, ShaderStageFlags type, Vk vk, Device device)
    {
        _filename = filename;
        _type = type;
        _vk = vk;
        _device = device;
    }

    internal PipelineShaderStageCreateInfo GetShaderStageCreateInfo()
    {
        var bytes = File.ReadAllBytes(_filename);
        ShaderModuleCreateInfo createInfo = new()
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)bytes.Length,
        };
        
        fixed (byte* codePtr = bytes)
        {
            createInfo.PCode = (uint*)codePtr;

            if (_vk!.CreateShaderModule(_device, in createInfo, null, out _module) != Result.Success)
            {
                throw new Exception();
            }
        }
        
        PipelineShaderStageCreateInfo shaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = _type,
            Module = _module,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };
        
        return shaderStageInfo;
    }
    
    public void Dispose()
    {
        _vk!.DestroyShaderModule(_device, _module, null);
        SilkMarshal.Free((nint)_pipelineShaderStageCreateInfo.PName);
    }
}