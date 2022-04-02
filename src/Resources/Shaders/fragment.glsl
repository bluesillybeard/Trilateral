#version 330

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex;

void main()
{
    vec2 newTexCoord = vec2(texCoord.x, 1 - texCoord.y);
    //invert the Y coordinate extremely simply in the GPU, rather than having to invert the image in the CPU.
    //Mostly because I was too lazy to do it in the CPU, i'ts just easier to invert the Y coordinage.
    outputColor = texture(tex, newTexCoord);
}