namespace VQoiSharp.Exceptions;

using System;
public class VQoiDecodingException : Exception
{
    public VQoiDecodingException(string message) : base(message)
    {
    }
}