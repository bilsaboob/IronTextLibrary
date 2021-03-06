﻿using System;
using IronText.Logging;

namespace IronText.Runtime
{
    /// <summary>
    /// Represents error caused by particular datum.
    /// </summary>
    public class SyntaxException : ApplicationException
    {
        private readonly string message;
        private readonly Loc location;
        private readonly HLoc hLocation;

        public SyntaxException(Loc location, HLoc hLocation, string message)
        {
            this.location = location;
            this.hLocation = hLocation;
            this.message = message;
        }

        /// <summary>
        /// Error message containing location, datum text and error explanation.
        /// </summary>
        public override string Message
        {
            get
            {
                string locationText;
                if (hLocation.IsUnknown)
                {
                    locationText = "";
                }
                else
                {
                    locationText = hLocation.ToString() + ": ";
                }

                return string.Format("{0}{1}", locationText, message);
            }
        }
    }
}
