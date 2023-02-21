#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Coretronic.Reality.Tools
{
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);        

            bool wasEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (!condHAtt.HideInInspector || enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }        

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (!condHAtt.HideInInspector || enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                //The property is not being drawn
                //We want to undo the spacing added before and after the property
                return -EditorGUIUtility.standardVerticalSpacing;
                //return 0.0f;
            }


            /*
            //Get the base height when not expanded
            var height = base.GetPropertyHeight(property, label);

            // if the property is expanded go through all its children and get their height
            if (property.isExpanded)
            {
                var propEnum = property.GetEnumerator();
                while (propEnum.MoveNext())
                    height += EditorGUI.GetPropertyHeight((SerializedProperty)propEnum.Current, GUIContent.none, true);
            }
            return height;*/
        }

        private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            bool enabled = (condHAtt.UseOrLogic) ?false :true;

            //Handle primary property
            SerializedProperty sourcePropertyValue = null;

            //Handle the unlimited property array
            string[] conditionalSourceFieldArray = condHAtt.ConditionalSourceFields;
            bool[] conditionalSourceFieldInverseArray = condHAtt.ConditionalSourceFieldInverseBools;
            for (int index = 0; index < conditionalSourceFieldArray.Length; ++index)
            {
                SerializedProperty sourcePropertyValueFromArray = null;
                if (!property.isArray)
                {
                    string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
                    string conditionPath = propertyPath.Replace(property.name, conditionalSourceFieldArray[index]); //changes the path to the conditionalsource property path
                    sourcePropertyValueFromArray = property.serializedObject.FindProperty(conditionPath);

                    //if the find failed->fall back to the old system
                    if (sourcePropertyValueFromArray == null)
                    {
                        //original implementation (doens't work with nested serializedObjects)
                        sourcePropertyValueFromArray = property.serializedObject.FindProperty(conditionalSourceFieldArray[index]);
                    }
                }
                else
                {
                    // original implementation(doens't work with nested serializedObjects) 
                    sourcePropertyValueFromArray = property.serializedObject.FindProperty(conditionalSourceFieldArray[index]);
                }

                //Combine the results
                if (sourcePropertyValueFromArray != null)
                {
                    bool propertyEnabled = CheckPropertyType(sourcePropertyValueFromArray);                
                    if (conditionalSourceFieldInverseArray.Length >= (index+1) && conditionalSourceFieldInverseArray[index]) propertyEnabled = !propertyEnabled;

                    if (condHAtt.UseOrLogic)
                        enabled = enabled || propertyEnabled;
                    else
                        enabled = enabled && propertyEnabled;
                }
                else
                {
                    //Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue found in object: " + condHAtt.ConditionalSourceField);
                }
            }


            //wrap it all up
            if (condHAtt.Inverse) enabled = !enabled;

            return enabled;
        }

        private bool CheckPropertyType(SerializedProperty sourcePropertyValue)
        {
            //Note: add others for custom handling if desired
            switch (sourcePropertyValue.propertyType)
            {                
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue;                
                case SerializedPropertyType.ObjectReference:
                    return sourcePropertyValue.objectReferenceValue != null;                
                default:
                    Debug.LogError("Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                    return true;
            }
        }
    }
}
#endif