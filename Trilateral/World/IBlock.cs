namespace Trilateral.World;

using System;
using vmodel;
using VRenderLib.Interface;

public interface IBlock
{
    VModel Model {get;}
    IRenderTexture Texture {get;}
    IRenderShader Shader {get;}
    bool Draw {get;}
    //localized to language
    string Name {get;}
    //the name of the block that is always the same no matter what
    string UUID {get;}
}