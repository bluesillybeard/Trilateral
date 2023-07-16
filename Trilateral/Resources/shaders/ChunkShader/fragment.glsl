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

uint xorshift323d(uint x, uint y, uint z)
{
    uint v = 0u;
    v ^= xorshift32(x);
    v ^= xorshift32(y);
    v ^= xorshift32(z);
    return v * v;
}

uint xorshift32extreme(uint v)
{
    v ^= v >> 17u;
    v ^= v << 13u;
    v ^= v >> 5u;
    v ^= v << 21u;
    v ^= v >> 7u;
    v ^= v << 27u;
    v ^= v >> 31u;
    v ^= v << 19u;
    v ^= v >> 11u;
    v ^= v << 3u;
    v ^= v >> 29u;
    v ^= v >> 25u;
    v ^= v << 1u;
    v ^= v << 15u;
    v ^= v >> 9u;
    v ^= v << 23u;
    v ^= v * 3u;
    return v;
}

uint xorshift32extreme3d(uint x, uint y, uint z)
{
    uint v = 0u;
    v ^= xorshift32extreme(x);
    v ^= xorshift32extreme(y);
    v ^= xorshift32extreme(z);
    //v = xorshift32extreme(v);
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
    v = ( v >> 16              ) | ( v                << 16);

    return v;
}


float bayerMatrix2D(uint i, uint j)
{
    const uint n = 256u;
    i = i % n;
    j = j % n;
    //this differs from the origial formula.
    // Probably because I did something wrong lol.
    // But, it produces an acceptable result.
    return reverseBits(interleave(i ^ j, i)) / float(4294967295u);
}

const float gammaScale = 5;
//GLSL isn't smart enough to know that 2 is a positive number...
// And yet, it is well aware that exp is purely functional.
// GLSL, why are you this way?
// (probably because it's a pretty old language, developed right near the beginning of GPU computation)
const float exps = exp(gammaScale);

float gammaCorrect(float input)
{
    // TODO: test if my weird function or actual gamma correction is faster.
    // Gamma correction is a simpler formula, but the exp() function might be extremely highly optimized.
    
    return (exp(gammaScale*input)-1)/(exps-1);
}

float getThreshold(vec3 input)
{
    float value = xorshift323d(floatBitsToUint(input.x), floatBitsToUint(input.y), floatBitsToUint(input.z)) / 4294967296.0;
    return 1 - gammaCorrect(value);

    // //Nobody's ever done a 3D bayer matrix before (at least not on the internet),
    // //so I was on my own in terms of figuring this out.
    // // GLSL is so annoying when using unsigned integers - it's too dumb to see that 2 is positive.
    // // Most languages with unsigned its are smart enough to see that. GLSL is not.
    // const uint depthScale = uint(1 >> 20);
    // uint x = uint(input.x);
    // uint y = uint(input.y);
    // uint z = xorshift32(floatBitsToUint(input.z));
    // //That by itself would work, however when multiple transparent objecs overlap,
    // // they tend to activate the same pixels, so fewer pixels are added than would normally make sense.
    // // So, one last modification tries to fix that.
    // x += z / depthScale;
    // y += xorshift32(z) / depthScale;
    
    // return bayerMatrix2D(x, y)/3 + bayerMatrix2D(x, z)/3 + bayerMatrix2D(y, z)/3;
}

out vec4 colorOut;
in vec2 texCoord;
in float depth;
uniform sampler2D tex;
void main(){
    colorOut = texture(tex, texCoord);
    //colorOut = vec4(vec3(getThreshold(vec3(gl_FragCoord.xy, depth))), 1);
    if(colorOut.a == 0 || (colorOut.a != 1 && colorOut.a < getThreshold(vec3(gl_FragCoord.xy, depth))))
    {
        discard;
    }
}