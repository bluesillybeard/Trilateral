using System;

namespace vmodel;

public enum VErrorType{
    Exception, vmfSyntax, unknown
}
public struct VError{
    Exception? exception;
    string message;
    VErrorType type;

    public VError(string parseError){
        type = VErrorType.vmfSyntax;
        message = parseError;
        exception = null;
    }

    public VError(Exception exception){
        this.type = VErrorType.Exception;
        message = exception.Message;
        this.exception = exception;
    }

    public override string ToString()
    {
        if(type == VErrorType.Exception){
            #pragma warning disable //disable the null error since if the type is Exception then the Exception member will be set.
            return "Exception: \"" + message + "\" \n Stacktrace: " + exception.StackTrace;
            #pragma warning enable
        }
        return type + ":\"" + message + "\"";
    }
}