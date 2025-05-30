#version 450

// binding 2 = our atlas combined‐image‐sampler
layout(set = 0, binding = 2) uniform sampler2D atlasSampler;

struct Material {
    vec3 diffuse;
    vec2 uvOffset;    
    vec2 atlasScale;  
};

layout(set = 0, binding = 1, std430)
readonly buffer Materials {
    Material mats[];
} materials;

// from the vertex shader
layout(location = 0) flat in uint  fragMatIndex;
layout(location = 1) in vec3  normal;
layout(location = 2) in vec2  uv;

layout(location = 0) out vec4 outColor;

void main() {
    // 1) Lighting parameters
    const vec3  lightDir   = normalize(vec3(0.8, 1.2, -0.6));
    const vec3  lightColor = vec3(1.0);      // white light
    const float ambient    = 0.1;            // ambient term
    
    
    // 2) Fetch material
    Material m = materials.mats[fragMatIndex];

//     3) Compute atlas UV (undo V-flip, then offset+scale)
    
    vec2 atlasUV   = m.uvOffset + uv * m.atlasScale;
    
    
    vec2 texSize = vec2(textureSize(atlasSampler,0));
    //atlasUV += (0.8 / texSize) * m.atlasScale;
    vec2 atlasPixelSpace = atlasUV * vec2(texSize);
    // 4) Sample your atlas texture
    vec4 texC = texture(atlasSampler, atlasUV);

    // 5) Simple Lambertian diffuse
    vec3 N     = normalize(normal);
    float diff = max(dot(N, -lightDir), 0.0);

    // 6) Combine: (ambient + diffuse) × albedo(tint×tex) × lightColor
    vec3 albedo  = m.diffuse * texC.rgb;
    vec3 shading = (ambient + 0.9 * diff) * lightColor;
    vec3 lit     = albedo * shading;

    // 7) Output with original alpha
    outColor = vec4(lit, texC.a);
//    outColor = vec4(color, 1.0);
    //–––– Uncomment to debug winding: green=front, red=back ––––
    //    outColor = gl_FrontFacing
    //        ? vec4(0,1,0,1)
    //        : vec4(1,0,0,1);
}
