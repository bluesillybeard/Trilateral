#version 330 core



//interleave code from https://graphics.stanford.edu/~seander/bithacks.html#InterleaveBMN
// I can't figure out const arrays in GLSL.
// I heard somewhere that it's explicitly disallowed, which makes zero sense to me at all.
// Thankfully, the guy who made the algorithm made the arrays quite pointlessly.
//const uint B[4] = (uint(0x55555555), uint(0x33333333), uint(0x0F0F0F0F), uint(0x00FF00FFu));
//const uint S[4] = (uint(1), uint(2), uint(4), uint(8));

// Interleave lower 16 bits of x and y, so the bits of x
// are in the even positions and bits from y in the odd;
// returns the resulting 32-bit Morton Number.
// x and y must initially be less than 65536.
uint interleave(uint x, uint y)
{
    x = (x | (x << 8u)) & 0x00FF00FFu;
    x = (x | (x << 4u)) & 0x0F0F0F0Fu;
    x = (x | (x << 2u)) & 0x33333333u;
    x = (x | (x << 1u)) & 0x55555555u;

    y = (y | (y << 8u)) & 0x00FF00FFu;
    y = (y | (y << 4u)) & 0x0F0F0F0Fu;
    y = (y | (y << 2u)) & 0x33333333u;
    y = (y | (y << 1u)) & 0x55555555u;

    return x | (y << 1);
}

uint xorshift32(uint v)
{
    v ^= v << 13u;
    v ^= v >> 17u;
    v ^= v << 5u;
    return v;
}

uint reverseBits(uint v)
{
    // swap odd and even bits
    v = ((v >> 1) & 0x55555555u) | ((v & 0x55555555u) << 1);
    // swap consecutive pairs
    v = ((v >> 2) & 0x33333333u) | ((v & 0x33333333u) << 2);
    // swap nibbles ... 
    v = ((v >> 4) & 0x0F0F0F0Fu) | ((v & 0x0F0F0F0Fu) << 4);
    // swap bytes
    v = ((v >> 8) & 0x00FF00FFu) | ((v & 0x00FF00FFu) << 8);
    // swap 2-byte long pairs
    v = ( v >> 16             ) | ( v               << 16);

    return v;
}

float bayerMatrix2D(uint i, uint j)
{
    const uint n = 8u;
    return reverseBits(interleave(i ^ j, i)) / (n * n);
}

float getThreshold(vec3 input)
{
    //return xorshift32(floatBitsToUint(input.x + input.y * 5 + input.z * 11)) / 4294967296.0;
    return bayerMatrix2D(uint((input.y)*65535), uint((input.x)*65535));
}

out vec4 colorOut;
in vec2 texCoord;
in vec3 screenPos;
uniform sampler2D tex;
void main(){
    colorOut = vec4(getThreshold(screenPos));//texture(tex, texCoord);
    
    //if(colorOut.a < getThreshold(screenPos))discard;
}