
#region <Developer Notes>
/*
 * Last Update: 2022, 04.04
 
 * Bug Fixes:
 *      - Fixed: Prevented Invalid Clipboard Data to be Set to the Control.
 *      - Fixed: (Occasionally) Impossible to Set the Decimals and Currency Designator when Changing the TextBox Input Type.
 *      - Fixed: Character Limiter Function was Missing.
 *      - Fixed: Placeholder Function was Missing.
 *      - Fixed: Fail to Set Decimals on a Single (the 1st TextBox)
 *
 * Known Issues:
 *      - Pasting Clipboard Data: Behaviour can be Improved.
 *      - There is an issue with Text Limiter. When using Decimals: TextBox has Text Input Improper Behaviour.
 *      - Sometimes not Accepting Paste. SHIFT + INSERT, on the other hand is allowed.
 *      - Initial (Default) Value Impossible to be Set.
 *      - Subscribing the "Leave" Event on a Form, Causes a Weird Behaviour.
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
 *            6. Placeholder (Not Implemented)
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

            // Placeholder Initial Configuration
            baseTextColor = ForeColor;
            baseTextFont = Font;
        }
        #endregion


        #region <Fields>
        // -> Number & Decimals (Configuration)
        private const int WM_PASTE = 0x0302;            // Used to Validate Clipboard Data.
        private string numbers = "0123456789.";         // Filter to Determine if Char Contains Number.
        private string allowedChars => numbers;         // Filter to Determine if Char Contains Number.
        private string decimalFormat = "0.00";          // Initial (Selected) Decimal Format.

        // -> Placeholder
        private bool isPlaceholder { get; set; }
        private Color baseTextColor { get; set; }
        private Font baseTextFont { get; set; }

        // -> Numeric Text (Properties)

        /// <summary> Retrieves TextBox Numeric Value String. </summary>
        public string NumericText => GetNumericString();
        /// <summary> Retrieves TextBox Numeric Value. </summary>
        public double NumericValue => GetNumericValue();



        #endregion


        #region <Custom Properties> : (1. Input Type)
        /// <summary> TextBox Text Input Type (Normal, Numeric, IPV4). </summary>
        public enum TextBoxInputType { Default, Numeric, IPV4 }
        private TextBoxInputType inputType = TextBoxInputType.Default;
        [Category("1. Custom Properties"), DisplayName("01. Input Type")]
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
        [Category("1. Custom Properties"), DisplayName("02. Use Currency")]
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
        [Category("1. Custom Properties"), DisplayName("03. Currency Designator")]
        [Description("Set Currency Symbol or Designator.\n\n i.e: €, Eur, Euros")]
        [Browsable(true)]
        public string CurrencyDesignator
        {
            get { return currencyDesignator; }
            set
            {
                // 1. Remove Current Currency Designator Before Updating to a New Value.
                Text_RemoveCurrency();

                // 2. Set the New Currency Designator
                currencyDesignator = value;

                // 3. Format the Currency Text.
                Text_FormatValue();

                Invalidate();
            }
        }

        public enum DesignatorAlignment { Left, Right }
        private DesignatorAlignment designatorAlignment = DesignatorAlignment.Right;
        [Category("1. Custom Properties"), DisplayName("04. Designator Location")]
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
        private bool useDecimals = false;
        [Category("1. Custom Properties"), DisplayName("05. Use Decimals")]
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
        [Category("1. Custom Properties"), DisplayName("06. Decimal Places")]
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
                        case 4: decimalFormat = "0.0000"; break;
                    }
                }

                Text_FormatValue();

                Invalidate();
            }
        }
        #endregion       

        #region <Custom Properties> : (4. Char Limiter)
        private bool charsLimited = false;
        [Category("1. Custom Properties"), DisplayName("07. Chars Limited")]
        [Description("Toggle Character Input Limiting.")]
        [Browsable(true)]
        public bool CharsLimited
        {
            get { return charsLimited; }
            set { charsLimited = value; }
        }

        private int maximumChars = 32;
        [Category("1. Custom Properties"), DisplayName("08. Maximum Chars")]
        [Description("Limit the Maximum Number of Chars Allowed on the Control.")]
        [Browsable(true)]
        public int MaximumChars
        {
            get { return maximumChars; }
            set { maximumChars = value; }
        }
        #endregion

        #region <Custom Properties> : (5. Placeholder)
        private bool usePlaceholder = false;
        [Category("1. Custom Properties"), DisplayName("09. Use Placeholder")]
        [Description("Toggle TextBox Placeholder Function.")]
        [Browsable(true)]
        public bool UsePlaceholder
        {
            get { return usePlaceholder; }
            set
            {
                usePlaceholder = value;

                TogglePlaceholder();

                Invalidate();
            }
        }

        private string placeHolderText = "Enter Text";
        [Category("1. Custom Properties"), DisplayName("10. Placeholder Text")]
        [Description("Set Placeholder Text.")]
        [Browsable(true)]
        public string PlaceholderText
        {
            get { return placeHolderText; }
            set
            {
                placeHolderText = value;

                Invalidate();
            }
        }

        private Color placeHolderForeColor = Color.DimGray;
        [Category("1. Custom Properties"), DisplayName("11. Placeholder ForeColor")]
        [Description("Set Placeholder Text.")]
        [Browsable(true)]
        public Color PlaceholderForeColor
        {
            get { return placeHolderForeColor; }
            set
            {
                placeHolderForeColor = value;

                Invalidate();
            }
        }

        private Font placeHolderFont = new Font("Consolas", 10, FontStyle.Italic);
        [Category("1. Custom Properties"), DisplayName("12. Placeholder Font")]
        [Description("Set Placeholder Font.")]
        [Browsable(true)]
        public Font PlaceholderFont
        {
            get { return placeHolderFont; }
            set
            {
                placeHolderFont = value;

                Invalidate();
            }
        }
        #endregion


        #region <Overriden Events>
        /// <summary> Occurs Before the Control Stops Being the Active Control. </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnValidating(CancelEventArgs e)
        {
            base.OnValidating(e);

            if (!DesignMode)
            {
                Text_FormatValue();
            }
        }

        /// <summary> Occurs when a Keyboard Key is Pressed. </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (!DesignMode)
            {
                if (!e.KeyChar.Equals((char)Keys.Back))
                {
                    switch (inputType)
                    {
                        case TextBoxInputType.Default: e.Handled = IsLimitingChars(Text.Length); break;
                        case TextBoxInputType.Numeric:
                            e.Handled = !HasValidNumericChar(e.KeyChar) ^ IsLimitingChars(Text.Length);
                            //if (e.KeyChar.Equals('.') & NrCharOccurrences('.') >= 1) { e.Handled = true; }
                            e.Handled = e.KeyChar.Equals('.') & NrCharOccurrences('.') >= 1;
                            break;
                    }
                }
            }

            TogglePlaceholder();
        }

        /// <summary> Occurs when the Control Becomes the Active Control. </summary>
        /// <param name="e"></param>
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);

            if (!DesignMode)
            {
                // Format the String Value
                Text_FormatValue();
                Text_RemoveCurrency();
                Text_RemoveWhiteSpaces();

                // Select the Text
                SelectAll();

                TogglePlaceholder();
            }
        }

        /// <summary> Occurs when the Control Stops Being the Active Control. </summary>
        /// <param name="e"></param>
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);

            if (!DesignMode)
            {
                Text_FormatValue();

                TogglePlaceholder();
            }
        }
        #endregion



        #region <Methods> : (Numeric String)
        /// <summary> Sets the Text Value as a Whole Number. </summary>
        private void Text_SetWholeNumber()
        {
            if (!string.IsNullOrEmpty(GetNumericString()) ^ !string.IsNullOrWhiteSpace(GetNumericString()))
            {
                int number = (int)GetNumericValue();
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
            string val = string.Empty;

            // Decimals Enabled
            if (useDecimals)
            {
                // String Contains a Value:
                if (!string.IsNullOrEmpty(GetNumericString()) & !string.IsNullOrWhiteSpace(GetNumericString()))
                {
                    decimal decVal = -1;

                    // Success:
                    // [Reference]: if (decimal.TryParse(Text, out decVal)) { val = decVal.ToString("0.00"); }
                    if (decimal.TryParse(Text, out decVal)) { val = decVal.ToString(decimalFormat); }

                    // else { /* FAIL */ }

                    // Set the Decimal Value as Text
                    Text = val;
                }

                // String is Null, Empty or White Space:
                else
                {
                    decimal decVal = -1;

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
                            //if (!Text.StartsWith(currencyDesignator))
                            //{ Text = $"{currencyDesignator} {GetNumericValue()}"; }
                            Text = $"{currencyDesignator} {GetNumericString()}";

                            break;

                        case DesignatorAlignment.Right:
                            //if (!Text.EndsWith(currencyDesignator))
                            //{ Text = $"{GetNumericValue()} {currencyDesignator}"; }
                            Text = $"{GetNumericString()} {currencyDesignator}";
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
                case TextBoxInputType.Numeric: TextAlign = HorizontalAlignment.Right; break;  // Default & Unchangeable Alignment
                case TextBoxInputType.IPV4: TextAlign = HorizontalAlignment.Center; break;  // Default & Unchangeable Alignment
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
            string val = string.Empty;

            switch (inputType)
            {
                case TextBoxInputType.Numeric:
                    Text_RemoveWhiteSpaces();
                    Text_RemoveCurrency();

                    // Ensure the Text Value is a Number
                    //if (IsNumericString(Text)) { val = Text; }
                    //else { val = "0"; }
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

        #region <Methods> : (Char Limiter)
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

        #region <Methods> : (Char Manipulation)
        /// <summary> Calculates the Nr. of Occurrences for the Specified Char Parameter. </summary>
        /// <param name="char"></param>
        /// <returns> The Number of the Received Char Parameter Occurrences Found in the TextBox Text. </returns>
        private int NrCharOccurrences(char @char)
        {
            return Text.Split(@char).Length - 1;
        }
        #endregion

        #region <Methods> : (Clipboard Control)
        /// <summary> Checks if the Clipboard Content Value is Valid. </summary>
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
        #endregion

        #region <Methods> : (IP Validation)
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

        #region <Methods> : (Placeholder)
        /// <summary> Toggle Placeholder. </summary>
        private void TogglePlaceholder()
        {
            if (usePlaceholder)
            {
                var hasEmptyText = string.IsNullOrEmpty(Text) & string.IsNullOrWhiteSpace(Text);

                switch (hasEmptyText)
                {
                    // Set Placeholder Text
                    case true:

                        if (!isPlaceholder)
                        {
                            Text = placeHolderText;
                            ForeColor = placeHolderForeColor;
                            Font = placeHolderFont;

                            isPlaceholder = true;
                        }
                        break;

                    case false:
                        if (isPlaceholder)
                        {
                            ForeColor = baseTextColor;
                            Font = baseTextFont;
                            Text = String.Empty;

                            isPlaceholder = false;
                        }
                        break;
                }
            }
        }
        #endregion

        #region <Methods> : (Text Formatting)
        /// <summary> Formats the Control Text with Proper Formatting for the Selected Input Type. </summary>
        private void Text_FormatValue()
        {
            switch (inputType)
            {
                case TextBoxInputType.Default: /* ... */ break;

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
