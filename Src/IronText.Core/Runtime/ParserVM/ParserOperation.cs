﻿namespace IronText.Runtime
{
    public enum ParserOperation : byte
    {
        // Unexpected token
        Fail        = 0,

        // Accept token and shift to the next state
        Shift       = 1,

        // Reduce rule and invoke rule action
        Reduce      = 2,

        // Return from processing production tree
        Return      = 3,

        // Resolve Shrodinger's token
        Resolve     = 4,

        // Fork on allowed alternatives of a Shrodinger's token
        Fork        = 5,

        // Success
        Accept      = 6,

        // Return control to the parser
        Exit        = 7,

        // Continue parser loop
        Restart     = 8,

        // Sets already known current state
        ForceState  = 9,

        InternalError = 10 
    }
}
