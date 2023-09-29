#version 450

layout(location=0)in vec3 inPos;
layout(location=1)in vec3 inColor;
layout(location=2)in vec2 inUV;
layout(location=3)in vec3 inNormal;

layout(set=0,binding=0)uniform UniformBufferObject{
  mat4 model;
  mat4 view;
  mat4 proj;
  vec4 lightPos;
  vec4 viewPos;
}ubo;

// layout(set=0,binding=0)uniform UBOScene
// {
  //   mat4 projection;
  //   mat4 view;
  //   vec4 lightPos;
  //   vec4 viewPos;
// }uboScene;

layout(push_constant)uniform PushConsts{
  mat4 model;
}primitive;

layout(location=0)out vec3 outNormal;
layout(location=1)out vec3 outColor;
layout(location=2)out vec2 outUV;
layout(location=3)out vec3 outViewVec;
layout(location=4)out vec3 outLightVec;

void main()
{
  outNormal=inNormal;
  outColor=inColor;
  outUV=inUV;
  gl_Position=ubo.proj*ubo.view*primitive.model*vec4(inPos.xyz,1.);
  // gl_Position=ubo.proj*ubo.view*ubo.model*vec4(inPos.xyz,1.);//*primitive.model
  // gl_Position=uboScene.projection*uboScene.view*vec4(inPos.xyz,1.);//*primitive.model
  
  vec4 pos=ubo.view*vec4(inPos,1.);
  outNormal=mat3(ubo.view)*inNormal;
  vec3 lPos=mat3(ubo.view)*ubo.lightPos.xyz;
  outLightVec=ubo.lightPos.xyz-pos.xyz;
  outViewVec=ubo.viewPos.xyz-pos.xyz;
}