using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Xml;
using Ionic.BZip2;
using Ionic.Zip;


namespace ConsoleApplication1
{
    class metaInfo
    {
        public string name;
        public string unit;
        public qcData qc;
    }

    class qcData
    {
        public string model;
        public string sensorHeight;
        public string calibration;
        public string calibrationType;
    }

    class waterBody
    {
        public string id;
        public string name;
        public string region;
        public string exactCoord;
        public string elevation;
        public string depth;
        public string depthCalibration;
        public string notes;
        public string siteName;
        public string longitude;
        public string latitude;
    }



    class Program
    {
        static List<metaInfo> metaData = new List<metaInfo>();
        static waterBody waterMeta = new waterBody();
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
            List<string> qcInfo = new List<string>();

            using (XmlWriter xmlWriter = XmlWriter.Create(Path.GetFileName(toSave) + "Meta.xml"))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("lernz-meta");
                xmlWriter.WriteStartElement("variables");
                foreach (metaInfo v in metaData)
                {
                    xmlWriter.WriteStartElement("variable");
                    xmlWriter.WriteElementString("name", v.name);
                    xmlWriter.WriteElementString("unit", v.unit);

                    xmlWriter.WriteStartElement("qc-information");
                    if(v.qc.model != null)
                    {

                        xmlWriter.WriteStartElement("qc-column");
                        xmlWriter.WriteElementString("parameter", "Model");
                        
                        xmlWriter.WriteElementString("value", v.qc.model);
                        xmlWriter.WriteEndElement();// qc-column
                    }
                    if(v.qc.calibration != null)
                    {

                        xmlWriter.WriteStartElement("qc-column");
                        xmlWriter.WriteElementString("parameter", "Calibration");
                        
                        xmlWriter.WriteElementString("value", v.qc.calibration);
                        xmlWriter.WriteEndElement();// qc-column
                    }
                    if(v.qc.calibrationType != null)
                    {

                        xmlWriter.WriteStartElement("qc-column");
                        xmlWriter.WriteElementString("parameter", "Calibration Type");
                        
                        xmlWriter.WriteElementString("value", v.qc.calibrationType);
                        xmlWriter.WriteEndElement();// qc-column
                    }
                    if(v.qc.sensorHeight != null)
                    {

                        xmlWriter.WriteStartElement("qc-column");
                        xmlWriter.WriteElementString("parameter", "Sensor height (m)");
                        
                        xmlWriter.WriteElementString("value", v.qc.sensorHeight);
                        xmlWriter.WriteEndElement();// qc-column
                    }
                    
                    xmlWriter.WriteEndElement();// qc-information
                    xmlWriter.WriteEndElement();// variable
                }
                xmlWriter.WriteEndElement(); // end variables

                xmlWriter.WriteStartElement("waterbodies");
                xmlWriter.WriteStartElement("waterbody");
                xmlWriter.WriteElementString("waterbody-id", waterMeta.id);
                xmlWriter.WriteElementString("waterbody-name", waterMeta.name);
                xmlWriter.WriteElementString("region", waterMeta.region);
                xmlWriter.WriteElementString("exact-coord", waterMeta.exactCoord);
                xmlWriter.WriteElementString("elevation", waterMeta.elevation);
                xmlWriter.WriteElementString("water-depth-measured", waterMeta.depth);
                xmlWriter.WriteElementString("water-calibration", waterMeta.depthCalibration);
                xmlWriter.WriteElementString("notes", waterMeta.notes);
                xmlWriter.WriteStartElement("site");
                xmlWriter.WriteElementString("site-name", waterMeta.siteName);
                xmlWriter.WriteElementString("longituide", waterMeta.longitude);
                xmlWriter.WriteElementString("latitude", waterMeta.latitude);

                xmlWriter.WriteEndElement();//site
                xmlWriter.WriteEndElement();//waterbody

                xmlWriter.WriteEndElement();//waterbodies
                xmlWriter.WriteEndElement();//lernz-meta
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(Path.GetFileName(toSave));
                zip.AddFile(Path.GetFileName(toSave) + "Meta.xml");
                zip.Save(Path.GetFileNameWithoutExtension(fileName) + ".zip");
            }
           
            File.Delete(toSave);
            Console.WriteLine("Endline = " + endLine);
            Console.WriteLine("variables = " + variableLine);
            Console.WriteLine("flag = " + flagLine);
            metaData = new List<metaInfo>();
            
        }

        static void readInWaterMeta()
        {
            var reader = new StreamReader("waterData.txt");
            reader.ReadLine();
            reader.ReadLine();
            waterMeta.id = reader.ReadLine().Substring(14);
            waterMeta.name = reader.ReadLine().Substring(16);
            waterMeta.region = reader.ReadLine().Substring(8);
            waterMeta.exactCoord = reader.ReadLine().Substring(13);
            waterMeta.elevation = reader.ReadLine().Substring(10);
            waterMeta.depth = reader.ReadLine().Substring(22);
            waterMeta.depthCalibration = reader.ReadLine().Substring(18);
            waterMeta.notes = reader.ReadLine().Substring(7);
            waterMeta.siteName = reader.ReadLine().Substring(11);
            waterMeta.longitude = reader.ReadLine().Substring(11);
            waterMeta.latitude = reader.ReadLine().Substring(11);
        }


        static void Main(string[] args)
        {

            string[] files = System.IO.Directory.GetFiles(Directory.GetCurrentDirectory(), "*.CNV");
            System.IO.Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Completed_Files");
            readInWaterMeta();
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
                    metaInfo temp = new metaInfo();
                    temp.name = "TmpWtr_v(degC)";
                    temp.unit = "degC";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Depth"))  //Sort out depth metadata when its all done
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
                    metaInfo ph = new metaInfo();
                    ph.name = "pH_v()";
                    ph.unit = " ";
                    ph.qc = new qcData();
                    ph.qc.sensorHeight = "0.2";
                    metaData.Add(ph);
                }
                else if (s.Contains("Density"))
                {
                    finalHeader += "Density_v()";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Density_v()";
                    temp.unit = " ";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("sbeox0Mg/L"))
                {
                    finalHeader += "DOconc_v(mg/L)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "DOconc_v(mg/L)";
                    temp.unit = "mg/L";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Attenuation"))
                {
                    finalHeader += "BmAtt_v(1/m)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "BmAtt_v(1/m)";
                    temp.unit = "1/m";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                } 
                else if (s.Contains("sbeox0PS"))
                {
                    finalHeader += "DOsat_v(%sat)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "DOsat_v(%sat)";
                    temp.unit = "%sat";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Conductivity"))
                {
                    finalHeader += "Cond_v(mS/cm)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Cond_v(mS/cm)";
                    temp.unit = "mS/cm";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Specific Conductance"))
                {
                    finalHeader += "CondSp_v(mS/cm)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "CondSp_v(mS/cm)";
                    temp.unit = "mS/cm";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Fluorescence"))
                {
                    finalHeader += "FlChl_v(RFU)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "FlChl_v(RFU)";
                    temp.unit = "RFU";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.5";
                    metaData.Add(temp);
                }
                else if (s.Contains("Beam Transmission"))
                {
                    finalHeader += "BmTran_v(%)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "BmTran_v(%)";
                    temp.unit = "%";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.3";
                    metaData.Add(temp);
                }
                else if (s.Contains("PAR/Irradiance"))
                {
                    finalHeader += "RadPAR_v(umol/m^2/s)";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "RadPAR_v(umol/m^2/s)";
                    temp.unit = "umol/m^2/s";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.7";
                    metaData.Add(temp);
                }
                else if (s.Contains("Salinity"))
                {
                    finalHeader += "Sal_v()";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Sal_v()";
                    temp.unit = " ";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else
                {
                    finalHeader += s;
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = s;
                    temp.unit = " ";
                    metaData.Add(temp);
                }
                counter++;
            }
            finalHeader = finalHeader.TrimEnd(new char[] { '\t' });
            return finalHeader;
        }


    }

    }