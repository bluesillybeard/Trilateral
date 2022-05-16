#version 330 core

layout(location = 0) in vec3 aPosition;

layout(location = 1) in vec2 aTexCoord;

layout(location = 2) in vec3 aNormal;

out vec2 texCoord;
out vec3 normal;

uniform mat4 model;
uniform mat4 camera;

void main(void)
{
    texCoord = aTexCoord;
    normal = aNormal;
    //Force it to always render on top, since this shader is ment for GUI and debug rendering, which should always be rendered on top of everything.
    gl_Position = vec4((vec4(aPosition, 1.0) * model).xy, 0.001, 1.0);
}
