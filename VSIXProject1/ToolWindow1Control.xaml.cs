namespace VSIXProject1
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using EnvDTE80;
    using EnvDTE;
    using System.Text.RegularExpressions;


    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    /// 


    public class CodeStatistic
    {
        public string nameOfFunction { get; set; }

        public string numOfKeyWords { get; set; }
        public string numOfLines { get; set; }
        public string numOfPureCode { get; set; }

        public CodeStatistic(string name, int keywords, int lines, int linecode)
        {
            nameOfFunction = name;
            numOfKeyWords = keywords.ToString();
            numOfLines = lines.ToString();
            numOfPureCode = linecode.ToString();
        }
    }

    public partial class ToolWindow1Control : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        /// 

        private bool cppFormat;
        private static string dictForC = @"\b(auto|double|int|struct|break|else|long|switch|case|enum|register|typedef|char|extern|return|union|const|float|short|unsigned|continue|for|signed|void|default|goto|sizeof|volatile|do|if|static|while|true|false)\b";

        private string dictForCpp = @"\b(auto|double|int|struct|break|else|long|switch|case|enum|register|typedef|char|extern|return|union|const|float|short|unsigned|continue|for|signed|void|default|goto|sizeof|volatile|do|if|static|while|true|false|alignas|alignof|and|and_eq|asm|bitand|bitor|bool|catch|char16_t|char32_t|class|compl|constexpr|const_cast|decltype|delete|dynamic_cast|explicit|export|friend|inline|mutable|namespace|new|noexcept|not|not_eq|nullptr|operator|or|or_eq|private|protected|public|reinterpret_cast|static_assert|static_cast|template|this|thread_local|throw|try|typeid|typename|using|virtual|wchar_t|xor|xor_eq)\b";

        private static string dictForFile;

        public ToolWindow1Control()
        {
            this.InitializeComponent();
           items = new List<CodeStatistic>();
           //listView.ItemsSource = items;
        }

          public static List<CodeStatistic> items;
        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        
        private void parseFunc(CodeFunction function, string args = "")
        {
            TextPoint beginline = function.GetStartPoint(vsCMPart.vsCMPartHeader);
            TextPoint endline = function.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter);
            string textFunc = beginline.CreateEditPoint().GetText(endline);
            int firstindex = 0;
            int lastindex = textFunc.LastIndexOf('}');
            string pattern1 = @"""([^\\""\r\n]*(\\""|\\\r\n|\\)?)*"; //кавычки. проверка строки
            //если мы в строке вида "stroka///// \\" с экранированными ковычками НАЧАЛО
            string pattern2 = @"(""|[^\\]\r\n)"; // перевод на следующую строку или закрылась двойная ковычка КОНЕЦ

            string pattern3 = @"\'(?:[^\\\'\r\n]*(\\\'|\\)?)*"; 
            //манипуляции внутри строки, ограниченной одинарными ковычками НАЧАЛО
            string pattern4 = @"(\'|\r\n)";// // или ' или конец строки или в строке  одинарная ковычка КОНЕЦ

            string singleComment = @"\/\/";
            //работает
            string pattern5 = @"(?:.*\\(\r\n))+.*";//строка и конец строки с переносом на новую строку и комменатрием на ней
            string pattern6 = @"(.*)(\n|\r)";// строчка+ конец  строки БЕЗ ПЕРЕНОСА


            string pattern7 = @"\/\*[\s\S]*?\*\/";//многострочный комментарий, должны выполняться единовременно => захват
            string pattern = @"("+ pattern1+ pattern2+ @"|"+ pattern3 + pattern4+@"|"+singleComment+"(" +pattern5+ @"|"+ pattern6 +")" + @"|" + pattern7+")";
            string uncommented= Regex.Replace(textFunc, pattern, "", RegexOptions.Multiline);
            uncommented = Regex.Replace(uncommented, @"^(?:\s)*\n", String.Empty, RegexOptions.Multiline);


            string forRegex = uncommented.Substring(0, uncommented.LastIndexOf('}'));
            firstindex = 0;
            lastindex = uncommented.LastIndexOf('}');
            int numOfKey = Regex.Matches(uncommented.Substring(firstindex, lastindex - firstindex), dictForFile).Count;
            args = (args != String.Empty) ? args : uncommented.Substring(0, uncommented.IndexOf('{'));
           // args = Regex.Replace(args, pattern, " ", RegexOptions.Multiline);
            args = Regex.Replace(args, @"^(?:\s)*\n", String.Empty, RegexOptions.Multiline);
            args = Regex.Replace(args, "  ", String.Empty, RegexOptions.Multiline);
            

            //args = uncommented.Substring(0, textFunc.LastIndexOf(')')+1);
            CodeStatistic tmp = new CodeStatistic(args, numOfKey, (endline.Line - beginline.Line)+1, Regex.Matches(uncommented, @"\n").Count);
           // items.Add(tmp);
            listView.Items.Add(tmp);
            // return (tmp);
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {



            DTE2 dte = VSIXProject1Package.GetGlobalService(typeof(DTE)) as DTE2;
            try
            {
                listView.Items.Clear();
                FileCodeModel currentFile = dte.ActiveDocument.ProjectItem.FileCodeModel;
                string fileExtention = Regex.Match(dte.ActiveDocument.Name, @"\.(cpp|c)").Value;
                if (fileExtention == ".c")
                {
                    dictForFile = dictForC;
                }
                else if (fileExtention == ".cpp")
                {
                    dictForFile = dictForCpp;
                }
                //else 

                foreach (CodeElement element in currentFile.CodeElements)
                {
                    if (element.Kind == vsCMElement.vsCMElementClass)
                    {
                        CodeClass curclass = element as CodeClass;
                        foreach (CodeElement classElem in element.Children)
                        {
                            if (classElem.Kind != vsCMElement.vsCMElementFunction) continue;

                            string args = classElem.FullName + '(';
                            if (((CodeFunction)classElem).Parameters.Count != 0)
                            {
                                foreach (CodeParameter argument in ((CodeFunction)classElem).Parameters)
                                {
                                    args += argument.Type.AsFullName + ' ' + argument.FullName + ',' + ' ';
                                }
                                args = args.Remove(args.Length - 2, 2);
                            }
                            args += ')';

                            parseFunc(classElem as CodeFunction, args);
                            //  ListViewItem listViewItem1 = new ListViewItem(new string[] { curview.nameOfFunction, curview.numOfKeyWords.ToString, curview.numOfLines.ToString, curview.numOfPureCode.ToString });
                            //  listView.Items.Add(curview);
                        }
                    }
                    else if (element.Kind == vsCMElement.vsCMElementFunction)
                        parseFunc(element as CodeFunction, "");
                   // listView.Items.Add(items);
                }

            }
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show(exception.ToString() + "Try again");
            }





        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}