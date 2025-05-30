#version 460

// ——— set 0: camera UBO ——————————————————————————————
layout(set = 0, binding = 0) uniform Camera {
    mat4 view;
    mat4 proj;
} cam;

// ——— set 1: per-instance SSBO ————————————————————————
// ——— define your instance‐data struct up front —————————————————
struct InstanceData {
    mat4 model;
    uint materialIndex;
    vec3 _pad0;
};

// ——— set 0, binding 0: SSBO of InstanceData[] ————————————
layout(std430, set = 0, binding = 3) readonly buffer InstanceBuffer {
    InstanceData instances[];
};
// ——— per-vertex inputs ——————————————————————————————
layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;

// ——— outputs to fragment stage ——————————————————————
layout(location = 0) flat out uint fragMatIndex;
layout(location = 1) out vec3  outNormal;
layout(location = 2) out vec2  outUV;

void main() {
    // Compute the index into your SSBO:
    //   firstInstance comes from your DrawIndexedIndirectCommand
    uint idx = gl_BaseInstance + gl_InstanceIndex;

    // Pull per-instance data:
    mat4 model        = instances[idx].model;
    fragMatIndex      = instances[idx].materialIndex;

    // Transform the vertex:
    vec4 worldPos     = model * vec4(inPosition, 1.0);
    gl_Position       = cam.proj * cam.view * worldPos;

    // Transform the normal:
    outNormal         = mat3(transpose(inverse(model))) * inNormal;

    // Pass the (unmodified) UV — 
    // if you need atlas adjustments you can apply them here:
    outUV             = inUV;
}

