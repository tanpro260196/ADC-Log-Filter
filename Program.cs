using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ADC_Log_Filter
{
    partial class LogFilter
    {
        [STAThread]
        static void Main()
        {
            List<string> csvexportdata = new List<string>();
            csvexportdata.Add("No.,Log,ADC FW Reading,Vadc (V),Vbulk (V),Vac RMS at Parakeet");
            //CALL FILE SELECTION FUNCTION
            List<FileInfo> fileselection = Get_filepath();
            if (fileselection.Count == 0)
                return;
            //Loop through all selected files.
            foreach (FileInfo file in fileselection)
            {
                //READ AND PROCESSED LOG CONTENT
                List<string> adc_readings = Readtext(file.FullName);
                if (adc_readings == null)
                {
                    continue;
                }
                csvexportdata.AddRange(adc_readings);
            }
            int i = 1;
            for (int x = 1; x < csvexportdata.Count(); x++)
            {
                csvexportdata[x] = i.ToString() + "," + csvexportdata[x];
                i=i+11;
                x = x + 11;
            }
            csvexportdata[1] = csvexportdata[1] + ",=[@[ADC FW Reading]]/310,=[@[Vadc (V)]]*64.7/2.7,=[@[Vbulk (V)]]*PI()/4";
            //PARSE AND EXPORT TO CSV
            Data_export(csvexportdata, fileselection[0].DirectoryName, "ADC_Value");
        }
        //THIS FUNCTION IDENTIFY THE BEGINNING AND END OF EACH LOOP IN THE LOG. 
        static List<string> Readtext(string file)
        {
            //THE LOG IS SPIT AND SAVED EACH LOOP TO THIS VARIABLE BELOW
            FileInfo inputfile = new FileInfo(file);
            List<string> adc_list = new List<string>();
            //Before processing, check if the file is readable and not in use by any other program.
            try
            {
                FileStream checkread = System.IO.File.OpenRead(@file);
                checkread.Close();
            }
            catch (System.IO.IOException)
            {
                //If the file cannot be read. Show an error box.
                string message_failed = "Cannot open " + inputfile.Name + Environment.NewLine + ". File is in use." + Environment.NewLine + Environment.NewLine + "Try again?";
                string caption_failed = "IO Error";
                MessageBoxButtons buttons_fail = MessageBoxButtons.AbortRetryIgnore;
                DialogResult result_fail;
                // Displays the MessageBox.
                result_fail = MessageBox.Show(message_failed, caption_failed, buttons_fail, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                switch (result_fail)
                {
                    case System.Windows.Forms.DialogResult.Retry:
                        return Readtext(file);
                    case System.Windows.Forms.DialogResult.Ignore:
                        return null;
                    case System.Windows.Forms.DialogResult.Abort:
                        if (System.Windows.Forms.Application.MessageLoop)
                        {
                            // WinForms app
                            System.Windows.Forms.Application.Exit();
                        }
                        else
                        {
                            // Console app
                            System.Environment.Exit(1);
                        }
                        break;
                }
            }

            //THEN READ THE CONTENT OF THE LOG FILE AND SAVE THEM TO A TEMP VARIABLE FOR PROCESSING.
            string[] text = System.IO.File.ReadAllLines(@file);
            int i;
            for (i = 0; i < text.Count(); i++)
            {
                //FIND LOOP START IDENTIFIER FIRST
                if (text[i].Contains("current ADC="))
                {
                    int datalocation = text[i].IndexOf("ADC=") + 4;
                    adc_list.Add(text[i].Replace(',',' ') + "," + text[i].Substring(datalocation, 3));
                    for (int j = 0; j < 11; j++)
                    {
                        adc_list.Add(" ");
                    }
                }
            }
            return adc_list;
        }
        //THIS FUNCTION HANDLE USER INPUT. IGNORE THIS.
        static List<FileInfo> Get_filepath()
        {
            bool pathcheck = false;
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            List<FileInfo> File = new List<FileInfo>();
            while (!pathcheck)
            {
                openFileDialog1.Multiselect = true;
                openFileDialog1.InitialDirectory = @"C:\BatteryTest";
                openFileDialog1.DefaultExt = "log";
                openFileDialog1.Title = "Select ALL syslog-xxx files";
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                openFileDialog1.Filter = "ADC Log Files (syslog-xxxx)|";
                DialogResult result = openFileDialog1.ShowDialog();
                if (result == DialogResult.Cancel)
                    break;
                foreach (string filename in openFileDialog1.FileNames)
                {
                    if (filename.Length == 0)
                        break;
                    pathcheck = true;
                    File.Add(new FileInfo(filename));

                }
            }
            openFileDialog1.Dispose();
            return File;
        }
        //THIS FUNCTION HANDLE DATA EXPORT.
        static void Data_export(List<string> data, string folderpath, string filename)
        {
            //CREATE AN ARRAY WITH ALL THE DATA YOU WANT TO EXPORT

            //Before writing to file. Check if file is writable or not.
            try
            {
                FileStream checkwrite = System.IO.File.OpenWrite(@Path.Combine(folderpath, filename + ".csv"));
                checkwrite.Close();
            }
            catch (System.IO.IOException)
            {
                string message_failed = "Cannot write to destination file. File is in use." + Environment.NewLine + Environment.NewLine + "Try again?";
                string caption_failed = "IO Error";
                MessageBoxButtons buttons_fail = MessageBoxButtons.AbortRetryIgnore;
                DialogResult result_fail;
                // Displays the MessageBox.
                result_fail = MessageBox.Show(message_failed, caption_failed, buttons_fail, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                switch (result_fail)
                {
                    case System.Windows.Forms.DialogResult.Retry:
                        Data_export(data, folderpath, filename);
                        return;
                    case System.Windows.Forms.DialogResult.Ignore:
                        return;
                    case System.Windows.Forms.DialogResult.Abort:
                        if (System.Windows.Forms.Application.MessageLoop)
                        {
                            // WinForms app
                            System.Windows.Forms.Application.Exit();
                        }
                        else
                        {
                            // Console app
                            System.Environment.Exit(1);
                        }
                        break;
                }
            }

            // Write the string array to a new file.
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(folderpath, filename + ".csv")))
            {
                foreach (string line in data)
                    outputFile.WriteLine(line);
            }

            //Show a success message box after export and offer to open the file on the spot.
            string message = "Data exported to " + folderpath + "\\" + filename + ".csv." + Environment.NewLine + Environment.NewLine + "Open exported file?";
            string caption = "Success!";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;
            result = MessageBox.Show(message, caption, buttons, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2, MessageBoxOptions.DefaultDesktopOnly);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(folderpath + "\\" + filename + ".csv.");
                return;
            }
            else
                return;
        }
    }
}
