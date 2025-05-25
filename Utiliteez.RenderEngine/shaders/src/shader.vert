#version 450

layout(binding = 0) uniform UBO {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

layout(location=0) in vec3 inPosition;

layout(location=1) in uint  inMaterialIndex;
layout(location=2) in vec3  inNormal;
layout(location=3) in vec2  inUV;

layout(location=0) flat out uint fragMatIndex;
layout(location=1) out vec3 normal;
layout(location=2) out vec2 uv;


void main() {
    gl_Position      = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragMatIndex     = inMaterialIndex;
    normal = mat3(transpose(inverse(ubo.model))) * inNormal;
    uv     = inUV;
}