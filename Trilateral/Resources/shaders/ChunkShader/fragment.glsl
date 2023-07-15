#version 330 core

uint xorshift32(uint v)
{
    v ^= v << 13u;
    v ^= v >> 17u;
    v ^= v << 5u;
    return v;
}

out vec4 colorOut;
in vec2 texCoord;
in vec3 screenPos;
uniform sampler2D tex;
void main(){
    colorOut = texture(tex, texCoord);
    uint randInput = floatBitsToUint(screenPos.x + screenPos.y * 5 + screenPos.z * 11);
    if(colorOut.a < (xorshift32(randInput) / 4294967296.0))discard;
}