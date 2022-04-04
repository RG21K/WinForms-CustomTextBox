
#region <Developer Notes>
/*
 * Last Update: 2022, 04.04
 
 * Bug Fixes:
 *      - Prevented Clipboard Data to be Set to the Control.
 *      - (Occasionally) Impossible to Set the Decimals and Currency Designator when Changing the TextBox Input Type.
 *      - Character Limiter Function was Missing.
 *
 * Known Bugs:
 *      - There is an issue with Text Limiter while using Decimals. TextBox Prevents user Text Input Proper Behaviour.
 *      - Sometimes not Accepting Paste. SHIFT + INSERT, on the other hand is allowed.
 *      - Default Initial (Default) Value Impossible to be Set.
 *      - Fails to Set Decimals on a Single (the 1st TextBox)
 *      - Subscribing the "Leave" Event on a Form, Causes a Weird Effect.
 *  
 * 
 * About:
 *      - This is the Default WinForms TextBox Design with Additional (Missing) Features.
 *
 *
 * Features:
 *      
 *      - Ability to:
 *            1. Set the TextBox Text Input Type for:
 *                - Text (Default);
 *                - Numeric Functionality (i.e: Whole Numbers or Decimal Numbers; with or without Currency);
 *                - IPv4 Addresses.
 *
 *            2. Set a Currency Designation (Symbol or Name). (i.e: '€' or 'EUR').
 *            3. Set the Currency Designator Location (Left or Right Side of the Number).
 *            4. Set Decimal Values (Decimal Zeros are added Automatically when Entering a Whole Number).
 *            5. Limit Maximum Character Input.
 *
 */
#endregion

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RG_Custom_Controls.Controls
{
    public class RGTextBoxI : TextBox
    {
        #region <Constructor>
        public RGTextBoxI()
        {
            // -> Set Default Configuration.
            ForeColor = Color.Gainsboro;
            BackColor = Color.FromArgb(255, 36, 36, 52);
            BorderStyle = BorderStyle.FixedSingle;

            // Text_FormatValue();
        }
        #endregion


        #region <Fields>
        private const int WM_PASTE = 0x0302;            // Used to Validate Clipboard Data.
        private string numbers = "0123456789.";
        private string allowedChars => numbers;
        private string decimalFormat = string.Empty;

        /// <summary> Retrieves TextBox Numeric Value String. </summary>
        public string NumericText => GetNumericString();
        /// <summary> Retrieves TextBox Numeric Value. </summary>
        public double NumericValue => GetNumericValue();
        #endregion


        #region <Custom Properties> : (1. Input Type)
        /// <summary> TextBox Text Input Type (Normal, Numeric, IPV4). </summary>
        public enum TextBoxInputType { Default, Numeric, IPV4 }
        private TextBoxInputType inputType = TextBoxInputType.Default;
        [Category("1. Custom Properties"), DisplayName("1. Input Type")]
        [Description("Select Control Input Type (Normal, Numeric or Currency).")]
        [Bindable(true)] /* Required for Enum Types */
        [Browsable(true)]
        public TextBoxInputType TextBoxType
        {
            get { return inputType; }
            set
            {
                inputType = value;
                Text = String.Empty;
                Text_FormatValue();

                Invalidate();
            }
        }
        #endregion

        #region <Custom Properties> : (2. Curency)
        private bool useCurrency = false;
        [Category("1. Custom Properties"), DisplayName("2. Use Currency")]
        [Description("Set the Control Text to be used as Coin Currency")]
        [Browsable(true)]
        public bool CurrencyEnabled
        {
            get { return useCurrency; }
            set 
            {
                useCurrency = value;

                Text_FormatValue();

                Invalidate();
            }
        }

        private string currencyDesignator = "€";
        [Category("1. Custom Properties"), DisplayName("3. Currency Designator")]
        [Description("Set Currency Symbol or Designator.\n\n i.e: €, Eur, Euros")]
        [Browsable(true)]
        public string CurrencyDesignator
        {
            get { return currencyDesignator; }
            set { currencyDesignator = value; }
        }

        public enum DesignatorAlignment { Left, Right }
        private DesignatorAlignment designatorAlignment = DesignatorAlignment.Right;
        [Category("1. Custom Properties"), DisplayName("4. Designator Location")]
        [Description("Select Currency Designator Location")]
        [Bindable(true)] /* Required for Enum Types */
        [Browsable(true)]
        public DesignatorAlignment DesignatorLocation
        {
            get { return designatorAlignment; }
            set 
            {
                designatorAlignment = value;
                Text_FormatValue();
                Invalidate();
            }
        }
        #endregion

        #region <Custom Properties> : (3. Decimals)
        private bool useDecimals;
        [Category("1. Custom Properties"), DisplayName("5. Use Decimals")]
        [Description("Select wether to use Whole Number or a Decimal Number.")]
        [Browsable(true)]
        public bool UseDecimals
        {
            get { return useDecimals; }
            set
            {
                useDecimals = value;

                Text_FormatValue();

                Invalidate();
            }
        }

        private int decimalPlaces = 2;
        [Category("1. Custom Properties"), DisplayName("6. Decimal Places")]
        [Description("Select wether to use Whole Number or a Decimal Number.")]
        [Browsable(true)]
        public int DecimalPlaces
        {
            get { return decimalPlaces; }
            set
            {
                if (value >= 1 & value <= 4)
                {
                    decimalPlaces = value;

                    // Aet Decimal Format
                    switch (decimalPlaces)
                    {
                        case 1: decimalFormat = "0.0"; break;
                        case 2: decimalFormat = "0.00"; break;
                        case 3: decimalFormat = "0.000"; break;
                        case 5: decimalFormat = "0.0000"; break;
                    }
                }

                Text_FormatValue();
                Invalidate();
            }
        }
        #endregion       

        #region <Custom Properties> : (4. Char Limiter)
        private bool charsLimited = false;
        [Category("1. Custom Properties"), DisplayName("7. Chars Limited")]
        [Description("Toggle Character Input Limit.")]
        [Browsable(true)]
        public bool CharsLimited
        {
            get { return charsLimited; }
            set { charsLimited = value; }
        }

        private int maximumChars = 32;
        [Category("1. Custom Properties"), DisplayName("8. Maximum Chars")]
        [Description("Limit the Maximum Number of Chars Allowed.")]
        [Browsable(true)]
        public int MaximumChars
        {
            get { return maximumChars; }
            set { maximumChars = value; }
        }
        #endregion

        
        #region <Overriden Events>
        /// <summary> Occurs Before the Control Stops Being the Active Control. </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnValidating(CancelEventArgs e)
        {
            base.OnValidating(e);

            Text_FormatValue();
        }

        /// <summary> Occurs when a Keyboard Key is Pressed. </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (!e.KeyChar.Equals((char)Keys.Back))
            {
                // Limit Number of Characters
                switch (inputType)
                {
                    // case TextBoxInputType.Default: e.Handled = IsLimitingChars(Text.Length); break;
                    case TextBoxInputType.Numeric:
                        e.Handled = !HasValidNumericChar(e.KeyChar) ^ IsLimitingChars(Text.Length);
                        if (e.KeyChar.Equals('.') & NrCharOccurrences('.') >= 1) { e.Handled = true; }
                        break;
                        // ...
                }
            }
        }

        /// <summary> Occurs when the Control Becomes the Active Control. </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);

            // Format the String Value
            // Text_FormatValue();
            Text_RemoveCurrency();
            Text_RemoveWhiteSpaces();

            // Select the Text
            // SelectAll();
            Select(0, Text.Length);
        }

        /// <summary> Occurs when the Control Stops Being the Active Control. </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);

            Text_FormatValue();
        }
        #endregion


        #region <Numeric String>

        /// <summary> Sets the Text Value as a Whole Number. </summary>
        private void Text_SetWholeNumber()
        {
            if (!string.IsNullOrEmpty(GetNumericString()) & /*^*/ !string.IsNullOrWhiteSpace(GetNumericString()))
            {
                int number = int.Parse(GetNumericString());
                Text = number.ToString();
            }

            else
            {
                Text = "0";
            }
        }

        /// <summary> Set Formatted Text Value (Whole Number or Decimal). </summary> // <<------------
        private void Text_ToggleDecimals()
        {
            // Decimals Enabled
            if (useDecimals)
            {
                // String Contains a Value:
                if (!string.IsNullOrEmpty(GetNumericString()) & !string.IsNullOrWhiteSpace(GetNumericString()))
                {
                    decimal decVal = -1;
                    string val = string.Empty;

                    // Success:
                    // [Reference]: if (decimal.TryParse(Text, out decVal)) { val = decVal.ToString("0.00"); }
                    if (decimal.TryParse(GetNumericString(), out decVal)) { val = decVal.ToString(decimalFormat); }

                    // else { /* FAIL */ }

                    // Set the Decimal Value as Text
                    Text = val;
                }

                // String is Null, Empty or White Space:
                else
                {
                    decimal decVal = -1;
                    string val = string.Empty;

                    // Success:
                    // [Reference]: if (decimal.TryParse(Text, out decVal)) { val = decVal.ToString("0.00"); }
                    if (decimal.TryParse("0", out decVal)) { val = decVal.ToString(decimalFormat); }

                    // else { /* FAIL */ }

                    // Set the Decimal Value as Text
                    Text = val;
                }
            }

            // Decimals Disabled
            else { Text_SetWholeNumber(); }
        }

        /// <summary> Toggle Currency Designator </summary>
        private void Text_SetCurrency()
        {
            // Note:
            // - Criteria: Selected TextBox Input Type Must be Numeric Before Calling this Method.

            // Insert Currency Designator
            if (useCurrency)
            {
                if (!string.IsNullOrEmpty(GetNumericString()) & !string.IsNullOrWhiteSpace(GetNumericString()))
                {
                    switch (designatorAlignment)
                    {
                        case DesignatorAlignment.Left:
                            if (!Text.StartsWith(currencyDesignator))
                            { Text = $"{currencyDesignator} {GetNumericString()}"; }
                            break;

                        case DesignatorAlignment.Right:
                            if (!Text.EndsWith(currencyDesignator))
                            { Text = $"{GetNumericString()} {currencyDesignator}"; }
                            break;
                    }
                }
            }

            // Remove Currency Designator
            else
            {
                Text = Text.Replace(currencyDesignator, string.Empty);
            }
        }

        /// <summary> Toggle Text Alignment (for Numeric and IP Input Types). </summary>
        private void Text_SetAlignment()
        {
            switch (inputType)
            {
                case TextBoxInputType.Numeric: TextAlign = HorizontalAlignment.Right;  break;  // Default & Unchangeable Alignment
                case TextBoxInputType.IPV4:    TextAlign = HorizontalAlignment.Center; break;  // Default & Unchangeable Alignment
            }
        }

        /// <summary> Removes White Spaces. </summary>
        private void Text_RemoveWhiteSpaces()
        {
            if (inputType.Equals(TextBoxInputType.Numeric))
            {
                Text = Text.Replace(" ", string.Empty);
            }
        }

        /// <summary> Removes Currency Designator. </summary>
        private void Text_RemoveCurrency()
        {
            if (inputType.Equals(TextBoxInputType.Numeric))
            {
                if (Text.Contains(currencyDesignator))
                {
                    Text = Text.Replace(currencyDesignator, "");
                }
            }
        }

        /// <summary> Checks if Specified Char Paramter is a Valid Numeric Character. </summary>
        /// <param name="char"></param>
        /// <returns> true if Received Char is a Number. </returns>
        private bool HasValidNumericChar(char @char)
        {
            return allowedChars.Contains(@char) | @char.Equals((char)Keys.Back);
        }

        /// <summary> Checks if Received String Parameter is a Number. </summary>
        /// <param name="value"></param>
        /// <returns> True if Received String Parameter is a Number. </returns>
        private bool IsNumericString(string value)
        {
            bool isNumeric = true;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (!HasValidNumericChar(c))
                {
                    isNumeric = false;
                    break;
                }
            }

            return isNumeric;

        }
        
        /// <summary> Retrieves Numeric String. </summary>
        /// <returns></returns>
        private string GetNumericString()
        {
            switch (inputType)
            {
                case TextBoxInputType.Numeric:
                    Text_RemoveWhiteSpaces();
                    Text_RemoveCurrency();
                    break;
            }

            return Text;
        }

        /// <summary> Retrieves Numeric Value. </summary>
        /// <returns></returns>
        private double GetNumericValue()
        {
            double value = 0;

            switch (inputType)
            {
                case TextBoxInputType.Numeric:
                    value = double.Parse(GetNumericString());
                    break;
            }

            return value;
        }
        #endregion

        #region <Char Limiter>
        /// <summary> Checks if the TextBox is Limiting the Maximum Number of Characters. </summary>
        /// <param name="textLength"></param>
        /// <returns> True if the TextBox is Limiting the Maximum Number Chars </returns>
        private bool IsLimitingChars(int textLength)
        {
            bool val = false;

            if (charsLimited)
            {
                switch (inputType)
                {
                    case TextBoxInputType.Default: val = Text.Length.Equals(maximumChars); break;
                    case TextBoxInputType.Numeric:

                        if (useDecimals)
                        {
                            // Note: '+1' Refers the '.' that Separates the Decimals
                            val = Text.Length.Equals(maximumChars + decimalPlaces + 1);
                        }

                        else { val = Text.Length.Equals(maximumChars); }
                        break;
                        // case TextBoxInputType.IPV4: break;
                }
            }

            return val;
        }
        #endregion

        #region <Char Manipulation>
        /// <summary> Calculates the Nr. of Occurrences for the Specified Char Parameter. </summary>
        /// <param name="char"></param>
        /// <returns> The Number of the Received Char Parameter Occurrences Found in the TextBox Text. </returns>
        private int NrCharOccurrences(char @char)
        {
            return Text.Split(@char).Length - 1;
        }
        #endregion

        #region <Clipboard Control>
        protected override void WndProc(ref Message m)
        {
            /*
             * Remarks: Handling Clipboard Data (Validate Data on Paste).
             * Adapted Code from: 'Thorarin'.
             * Source: https://stackoverflow.com/questions/15987712/handle-a-paste-event-in-c-sharp
             */

            // 1. Handle All Other Messages Normally.
            if (m.Msg != WM_PASTE) { base.WndProc(ref m); }

            // 2. Handle Clipboard Data (On Paste).
            else
            {
                if (Clipboard.ContainsText())
                {
                    string val = Clipboard.GetText();

                    if (HasValidClipboardContent(val)) { Text = val; }

                    // Note(s):
                    // Text Validation for Each Input Type, Occurs under Control Leave Event.

                    // Clipboard.Clear(); --> You can use this if you Wish to Clear the Clipboard after Pasting the Value
                }
            }
        }

        /// <summary> Determines if the Clipboard Content Value is Valid. </summary>
        /// <param name="val"></param>
        /// <returns> True if Clipboard Content Matches the TextBox Input Requirements. </returns>
        private bool HasValidClipboardContent(string val)
        {
            bool isValid = false;

            switch (inputType)
            {
                case TextBoxInputType.Default: isValid = !IsLimitingChars(val.Length); break;
                case TextBoxInputType.Numeric:
                    isValid = !IsLimitingChars(val.Length) && IsNumericString(val);
                    break;

                case TextBoxInputType.IPV4:
                    isValid = HasValidIPAddress(val);
                    break;
            }

            return isValid;
        }
        #endregion

        #region <IP Validation>
        /// <summary> Checks if String Contains a Valid IPv4 Address. </summary>
        /// <returns> True if the IPv4 Address is Valid. </returns>
        private bool HasValidIPAddress(string value)
        {
            // Remarks:
            // Code based on Yiannis Leoussis Approach.
            // Using a 'for' Loop instead of 'foreach'.
            // Link: https://stackoverflow.com/questions/11412956/what-is-the-best-way-of-validating-an-ip-address

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(Text)) { isValid = false; }

            //  Split string by ".", check that array length is 4
            string[] arrOctets = Text.Split('.');

            if (arrOctets.Length != 4) { isValid = false; }

            // Check Each Sub-String (Ensure that it Parses to byte)
            byte obyte = 0;

            for (int i = 0; i < arrOctets.Length; i++)
            {
                string strOctet = arrOctets[i];

                if (!byte.TryParse(strOctet, out obyte)) { isValid = false; }
            }

            // Set Default TextBox Text if IP is Invalid:
            // if (!isValid) { Text_FormatValue(); } // <-- DUPLICATE METHOD ENTRY ------------

            return isValid;
        }
        #endregion

        #region <Text Formatting> : (Update the Text Value with Proper Formatting)
        /// <summary> Format the Text Value. </summary>
        private void Text_FormatValue()
        {
            switch (inputType)
            {
                case TextBoxInputType.Numeric:

                    Text_ToggleDecimals();  // Toggle Decimals
                    Text_SetCurrency();     // Toggle Currency
                    Text_SetAlignment();    // Set the Text Alignment (Numeric and IPv4)

                    break;

                case TextBoxInputType.IPV4: Text = "0.0.0.0"; break;
            }
        } 
        #endregion

    }
}