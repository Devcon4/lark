#version 450

layout (set = 1, binding = 0) uniform sampler2D samplerColorMap;

layout (location = 0) in vec3 inNormal;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec3 inViewVec;
layout (location = 4) in vec3 inLightVec;

layout (location = 0) out vec4 outFragColor;
layout (location = 1) out vec4 outNormal;

void main() 
{
  outNormal = vec4(inNormal, 1.0);
  // outNormal = vec4(0,1,0, 1);
	vec4 color = texture(samplerColorMap, inUV);
  outFragColor = vec4(color.xyz, 1);

  // outFragColor = vec4(inNormal.xyz, 1.0);

  // outFragColor = color;
  // outFragColor = vec4(1,0,0,1);

	// vec3 N = normalize(inNormal);
	// vec3 L = normalize(inLightVec);
	// vec3 V = normalize(inViewVec);
	// vec3 R = reflect(L, N);
	// vec3 diffuse = max(dot(N, L), 0.15) * inColor;
	// vec3 specular = pow(max(dot(R, V), 0.0), 16.0) * vec3(0.75);
	// outFragColor = vec4(diffuse * color.rgb + specular, 1.0);		
  // outFragColor = vec4(0, 0, 1, 1);
  // outFragColor = vec4(inNormal, 1.0);
}