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
    //The depth is in "screen space". Think of it like the screen has an imaginary third dimention.
    // I don't use gl_FragCoord since that gives me the coordinates in pixels, for whatever reason.
    // (I don't want pixels! I want normalized coordinates!)
    // gl_Position is in clip space, which took me about an hour to figure out lol.
    // This is why I should have payed more attention when I was reading learnopengl.com...
    screenPos = gl_Position.xyz / gl_Position.w;
}
