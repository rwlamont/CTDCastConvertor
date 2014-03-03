using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using Ionic.BZip2;
using Ionic.Zip;


namespace ConsoleApplication1
{
   
    class Program
    {
        
        static int depthHeader = 0;
        static bool firstDepth = false;
        static double offset = 0.0;
        static void Run(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            int variableLine = 0, flagLine = 0, endLine = 0;
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Equals("*END*"))
                {
                    endLine = i + 1;
                    break;
                }
            }
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains("name 0"))
                {
                    variableLine = i + 1;
                    break;
                }
            }
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains("flag"))
                {
                    flagLine = i + 1;
                    break;
                }
            }
            List<string> variables = new List<string>();
            for (int i = variableLine - 1; i < flagLine - 1; i++)
            {
                variables.Add(splitVariable(lines[i]));
            }

            
            var toSave = Directory.GetCurrentDirectory() + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_Data.txt";
            var writer = new StreamWriter(toSave);
            writer.WriteLine(parseHeader(variables));
            for (int i = endLine; i < lines.Count(); i++)
            {
                writer.WriteLine(cleanData(lines[i]));
                variableLine++;
            }
            writer.Close();

            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(Path.GetFileName(toSave));
                zip.Save(Path.GetFileNameWithoutExtension(fileName) + ".zip");
            }
           
            File.Delete(toSave);
            Console.WriteLine("Endline = " + endLine);
            Console.WriteLine("variables = " + variableLine);
            Console.WriteLine("flag = " + flagLine);
            
        }
        static void Main(string[] args)
        {

            string[] files = System.IO.Directory.GetFiles(Directory.GetCurrentDirectory(), "*.CNV");
            System.IO.Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Completed_Files");
            for (int j = 0; j < files.Count(); j++)
            {
                Console.WriteLine("Started processing " + files[j]);
                Run(files[j]);
                Console.WriteLine("Finished processing " + files[j]);
                Console.WriteLine();
            }
            Console.WriteLine("All files finished processing ");
            Console.WriteLine("Read a total of " + files.Count() +" files");
            Console.ReadLine();


        }

        static string splitVariable(string s)
        {
            string[] split = s.Split('=');
            return split[1].Trim();
        }

        static string cleanData(string st)
        {
            string toReturn = "";
            int counter = 0;
            
           
            string[] splitter = st.Split(' ');
            for (int i = 0; i < splitter.Count(); i++)
            { 
                if (splitter[i] != "")
                {
                    if (counter == depthHeader)
                    {
                        if (!firstDepth)
                        {
                            offset = double.Parse(splitter[i]);
                            firstDepth = true;
                        }
                        double parsed = double.Parse(splitter[i]);
                        double value = parsed - offset;
                        toReturn += splitter[i];
                        toReturn += "\t";
                        toReturn += value;
                        toReturn += "\t";
                        counter++;
                        continue;
                        
                    }

                    if (splitter[i].Equals("0.000e+00"))
                    {
                        continue;
                    }
                    else
                    {
                        toReturn += splitter[i];
                        toReturn += "\t";
                    }
                    counter++;
                }
            }
            toReturn = toReturn.TrimEnd(new char[] { '\t' });
            return toReturn;
        }

        static string parseHeader(List<string> headers)
        {
            bool first = false;
            int counter = 0;
            string finalHeader = "";
            foreach (string s in headers)
            {
                if(s.Contains("Temperature"))
                {
                    finalHeader += "TmpWtr_v(degC)";
                    finalHeader += "\t";   
                }
                else if (s.Contains("Depth"))
                {
                    finalHeader += "Depth_(m)";
                    finalHeader += "\t";
                    if (!first)
                    {
                        depthHeader = counter;
                        first = true;
                        finalHeader += "DepthAdj_(m)";
                        finalHeader += "\t";
                    }
                }
                else if (s.Contains("pH"))
                {
                    finalHeader += "pH_v()";
                    finalHeader += "\t";
                }
                else if (s.Contains("Density"))
                {
                    finalHeader += "Density_v()";
                    finalHeader += "\t";
                }
                else if (s.Contains("sbeox0Mg/L"))
                {
                    finalHeader += "DOconc_v(mg/L)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Attenuation"))
                {
                    finalHeader += "BmAtt_v(1/m)";
                    finalHeader += "\t";
                } 
                else if (s.Contains("sbeox0PS"))
                {
                    finalHeader += "DOsat_v(%sat)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Conductivity"))
                {
                    finalHeader += "Cond_v(mS/cm)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Specific Conductance"))
                {
                    finalHeader += "CondSp_v(mS/cm)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Fluorescence"))
                {
                    finalHeader += "FlChl_v(RFU)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Beam Transmission"))
                {
                    finalHeader += "BmTran_v(%)";
                    finalHeader += "\t";
                }
                else if (s.Contains("PAR/Irradiance"))
                {
                    finalHeader += "RadPAR_v(umol/m^2/s)";
                    finalHeader += "\t";
                }
                else if (s.Contains("Salinity"))
                {
                    finalHeader += "Sal_v()";
                    finalHeader += "\t";
                }
                else
                {
                    finalHeader += s;
                    finalHeader += "\t";
                }
                counter++;
            }
            finalHeader = finalHeader.TrimEnd(new char[] { '\t' });
            return finalHeader;
        }


    }

    }

