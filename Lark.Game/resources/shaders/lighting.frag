#version 450

layout (set = 0, binding = 0) uniform sampler2D geometryBuffer;
layout (set = 0, binding = 1) uniform sampler2D normalBuffer;
layout (set = 0, binding = 2) uniform sampler2D depthBuffer;

layout (location = 0) in vec2 fragTexCoord;
layout (location = 0) out vec4 outFragColor;

struct Light {
  vec4 color;
  vec4 position;
  vec4 options;
};

layout(set = 1, binding = 0) buffer lightBuffer {
  Light light[128];
}lightData;

layout(push_constant) uniform PushConstants {
  mat4 invView;
  mat4 invProj;
  vec4 cameraPos;
  int lightIndex;
} pushConstants;

vec3 clipToWorld(vec4 clip) {
  vec4 ndc = clip / clip.w;
  vec4 view = pushConstants.invProj * ndc;
  view.z = -1;
  view.w = 0;
  vec4 world = pushConstants.invView * view;
  return world.xyz;
}

// vec4 drawLight(Light currentLight, vec3 worldPos, vec4 normal) {
//   float lightIntensity = currentLight.color.a;

//   vec3 N = normalize(normal.xyz);
//   vec3 L = normalize(currentLight.position.xyz - worldPos);
//   vec3 V = normalize(pushConstants.cameraPos.xyz - worldPos);
//   vec3 R = reflect(-L, N);

//   float diff = max(dot(N, L), 0.0);
//   vec3 diffuse = diff * currentLight.color.rgb;

//   float spec = pow(max(dot(R, V), 0.0), 16.0);
//   vec3 specular = spec * currentLight.color.rgb;

//   vec3 finalColor = diffuse + specular;
//   return vec4(finalColor, 1);
// }

vec4 drawLight(Light currentLight, vec3 worldPos, vec4 normal) {
  float intensity = currentLight.options.x;
  float range = currentLight.options.y;
  float angle = currentLight.options.z;

  vec3 N = normalize(normal.xyz);
  vec3 L = normalize(currentLight.position.xyz - worldPos);
  vec3 V = normalize(pushConstants.cameraPos.xyz - worldPos);
  vec3 R = reflect(-L, N);

  float diff = max(dot(N, L), 0.0);
  vec3 diffuse = diff * currentLight.color.rgb * intensity;

  float spec = pow(max(dot(R, V), 0.0), 16.0);
  vec3 specular = spec * currentLight.color.rgb * intensity;

  vec3 lightNormal = normalize(currentLight.position.xyz - worldPos);

  // Calculate attenuation for point and spot lights
  float distance = length(lightNormal);
  float attenuation = clamp(1.0 - (distance / range), 0.0, 1.0);

  // Calculate spot effect
  float theta = dot(L, lightNormal);
  float epsilon = angle;
  float spotEffect = clamp((theta - epsilon) / (1.0 - epsilon), 0.0, 1.0);

  // Use step to determine light type and mix to blend values
  float isDirectional = step(0.0, range) * step(0.0, angle);
  float isPoint = step(0.0, angle) * (1.0 - isDirectional);
  float isSpot = 1.0 - isDirectional - isPoint;

  attenuation = mix(1.0, attenuation, isPoint + isSpot);
  spotEffect = mix(1.0, spotEffect, isSpot);

  diffuse *= attenuation * spotEffect;
  specular *= attenuation * spotEffect;

  vec3 finalColor = diffuse + specular;
  return vec4(finalColor, 1);
}

// Basic phong lighting model
void main() 
{

	vec4 geo = texture(geometryBuffer, fragTexCoord);
  vec4 normal = texture(normalBuffer, fragTexCoord);
  vec4 depth = texture(depthBuffer, fragTexCoord);
  
  vec4 clipSpacePos = vec4(fragTexCoord * 2 - 1, depth.r * 2 - 1, 1);
  vec3 worldPos = clipToWorld(clipSpacePos);

  // Light currentLight = lightData.light[pushConstants.lightIndex];

  // if (pushConstants.lightIndex == 0) {
  //   // red
  //   currentLight.color = vec4(1, 0, 0, 1);
  // }

  // if (pushConstants.lightIndex == 1) {
  //   // cyan
  //   currentLight.color = vec4(0, 1, 1, 1);
  // }

  // if (pushConstants.lightIndex == 2) {
  //   // yellow
  //   currentLight.color = vec4(1, 1, 0, 1);
  // }

  // vec4 render = drawLight(currentLight.color, currentLight.position, currentLight.options, worldPos, normal);

  // outFragColor = render;

  vec4 result = vec4(0, 0, 0, 0);

  for(int i=0;i<128;++i) // 128 is the max number of lights
  {
    // if (i != pushConstants.lightIndex) {
    //   continue;
    // }

    Light currentLight = lightData.light[i];
    
    // Only draw lights that are active
    if (currentLight.color.a == 0) {
      break;
    }
    vec4 render = drawLight(currentLight, worldPos, normal);

    result += render;
  }

  outFragColor = normalize(result);

  // light.color = vec4(1, 1, 0, 1);
  // if (length(light.color.rgb) == 0) {
  //   // set to pink if no light data
  //   light.color = vec4(1, 0, 1, 1);
  // }

  // light.position = vec4(0, 0, 5, 1);

  
  // outFragColor += vec4(light.color.rgb, 0.5);

  // outFragColor = vec4(normal.xyz, 1);
}