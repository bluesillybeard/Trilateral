#version 330 core
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

const float gammaScale = 5;
//GLSL isn't smart enough to know that 2 is a positive number...
// And yet, it is well aware that exp is purely functional.
// GLSL, why are you this way?
// (probably because it's a pretty old language, developed right near the beginning of GPU computation)
const float exps = exp(gammaScale);
float gammaCorrect(float input)
{
    // TODO: test if my weird function or actual gamma correction is faster.
    // Gamma correction is a simpler formula, but the exp() function might be extremely highly optimized
    return (exp(gammaScale*input)-1)/(exps-1);
}

float getThreshold(vec3 input)
{
    float value = xorshift323d(floatBitsToUint(input.x), floatBitsToUint(input.y), floatBitsToUint(input.z)) / 4294967296.0;
    return 1 - gammaCorrect(value);
}

out vec4 colorOut;
in vec2 texCoord;
in float depth;
uniform sampler2D tex;
void main(){
    colorOut = texture(tex, texCoord);
    if(colorOut.a == 0 || (colorOut.a != 1 && colorOut.a < getThreshold(vec3(gl_FragCoord.xy, depth))))
    {
        discard;
    }
}