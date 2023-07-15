#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 textureCoords;
layout(location = 2) in vec3 normal;
out vec2 texCoord;
out vec3 screenPos;
uniform mat4 camera;
uniform mat4 model;
void main(){
    texCoord = textureCoords;
    gl_Position = vec4(position, 1.0) * model * camera;
    screenPos = gl_Position.xyz;
}
