#version 450

layout (set = 0, binding = 0) uniform sampler2D geometryBuffer;
layout (set = 0, binding = 1) uniform sampler2D lightBuffer;
layout (set = 0, binding = 2) uniform sampler2D uiBuffer;

layout (location = 0) in vec2 fragTexCoord;
layout (location = 0) out vec4 outFragColor;

void main() 
{
  vec2 flippedCoord = vec2(fragTexCoord.x, 1-fragTexCoord.y);
	vec4 geo = texture(geometryBuffer, fragTexCoord);
  vec4 light = texture(lightBuffer, fragTexCoord);
  vec4 ui = texture(uiBuffer, fragTexCoord);
  // outFragColor = (geo + light) /2;
  // outFragColor = geo;
  // outFragColor = light;

  // Combine the geometry and light
  vec4 scene = geo + light;

  // The UI should be drawn on top of the scene
  vec4 withUI = mix(scene, ui, ui.a);

  outFragColor = withUI;
}