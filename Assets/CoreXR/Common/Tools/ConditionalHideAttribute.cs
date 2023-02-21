using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Coretronic.Reality.Tools
{
    [AttributeUsage(AttributeTargets.Field 
        | AttributeTargets.Property 
        | AttributeTargets.Class 
        | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        public string[] ConditionalSourceFields = new string[] { };
        public bool[] ConditionalSourceFieldInverseBools = new bool[] { };
        public bool HideInInspector = false;
        public bool Inverse = false;
        public bool UseOrLogic = false;

        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }

        public ConditionalHideAttribute(string conditionalSourceFields, 
                                        string conditionalSourceFieldInverseBools, 
                                        bool hideInInspector = false, 
                                        bool inverse = false)
        {
            this.ConditionalSourceFields = RemoveWhitespace(conditionalSourceFields).Split(',');
            this.ConditionalSourceFieldInverseBools = RemoveWhitespace(conditionalSourceFieldInverseBools)
                .Split(',').Select(bool.Parse).ToArray();
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceFields, bool hideInInspector = false, bool inverse = false)
        {
            this.ConditionalSourceFields = conditionalSourceFields.Split(',');        
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }
    }
}
