#version 330 core

layout(location = 0) in vec2 aPosition;

layout(location = 1) in vec2 aTexCoord;

layout(location = 2) in vec4 aColor;

out vec2 texCoord;
out vec4 color;

uniform mat4 model;
uniform mat4 camera;

void main(void)
{
    texCoord = aTexCoord;
    color = aColor;

    gl_Position = vec4(aPosition, 0.0, 1.0) * model * camera;
}
