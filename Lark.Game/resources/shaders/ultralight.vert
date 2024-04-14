#version 450

layout(location=0)out vec2 fragTexCoord;

void main(){
  vec4 positions[6]=vec4[](
    vec4(-1.,-1.,0.,1.),// Bottom-left
    vec4(1.,-1.,0.,1.),// Bottom-right
    vec4(-1.,1.,0.,1.),// Top-left
    vec4(1.,-1.,0.,1.),// Bottom-right
    vec4(1.,1.,0.,1.),// Top-right
    vec4(-1.,1.,0.,1.)// Top-left
  );
  
  vec2 texCoords[6]=vec2[](
    vec2(0.,0.),// Corresponds to Bottom-left vertex
    vec2(1.,0.),// Corresponds to Bottom-right vertex
    vec2(0.,1.),// Corresponds to Top-left vertex
    vec2(1.,0.),// Corresponds to Bottom-right vertex
    vec2(1.,1.),// Corresponds to Top-right vertex
    vec2(0.,1.)// Corresponds to Top-left vertex
  );
  
  gl_Position=positions[gl_VertexIndex];
  fragTexCoord=texCoords[gl_VertexIndex];
}