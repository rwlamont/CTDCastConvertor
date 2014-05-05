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
        public string notes;
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
        static string[] calibDates = new string[30];
        static int depthHeader = 0;
        static bool firstDepth = false;
        static double offset = 0.0;
        static int localLength = 0; // Length of calib date
        static int startPoint = 0; // Start of Calib date
        static int dateCounter = 0; // numbe of calib dates
        static string waterType = "Lake";
        static void Run(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            dateCounter = 0;
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
                if (lines[i].Contains("CalibrationDate"))
                {
                    startPoint = lines[i].IndexOf(">") + 1;
                    localLength = (lines[i].Length - 18) - startPoint;
                    calibDates[dateCounter] = lines[i].Substring(startPoint, localLength);
                    dateCounter++;
                    
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
            
            
           

            using (XmlWriter xmlWriter = XmlWriter.Create("LernzMeta.xml"))
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
                    if (v.name == "DepthAdj")
                    {
                        v.qc.notes = "Adjusted by an offset of " + offset + " Meters.";
                    }
                    if (v.qc.notes != null)
                    {

                        xmlWriter.WriteStartElement("qc-column");
                        xmlWriter.WriteElementString("parameter", "Notes");

                        xmlWriter.WriteElementString("value", v.qc.notes);
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

            try
            {
                
                using (XmlWriter xmlWrite = XmlWriter.Create("mets.xml"))
                {
                    xmlWrite.WriteStartDocument();
                    xmlWrite.WriteRaw("\n<mets ID=\"sort-mets_mets\" OBJID=\"sword-mets\" LABEL=\"DSpace SWORD Item\" PROFILE=\"DSpace METS SIP Profile 1.0\" xmlns=\"http://www.loc.gov/METS/\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.loc.gov/METS/ http://www.loc.gov/standards/mets/mets.xsd\">\n");
                    xmlWrite.WriteRaw("\t<metsHdr CREATEDATE=\"" + DateTime.UtcNow + "\">\n");
                    xmlWrite.WriteRaw("\t\t<agent ROLE=\"CUSTODIAN\" TYPE=\"ORGANIZATION\">\n");
                    xmlWrite.WriteRaw("\t\t\t<name>Unkown</name>\n");
                    xmlWrite.WriteRaw("\t\t</agent>\n");
                    xmlWrite.WriteRaw("\t</metsHdr>\n");
                    xmlWrite.WriteRaw("<dmdSec ID=\"sword-mets-dmd-1\" GROUPID=\"sword-mets-dmd-1_group-1\">\n");
                    xmlWrite.WriteRaw("<mdWrap LABEL=\"SWAP Metadata\" MDTYPE=\"OTHER\" OTHERMDTYPE=\"EPDCX\" MIMETYPE=\"text/xml\">\n");
                    xmlWrite.WriteStartElement("xmldata");
                    xmlWrite.WriteRaw("<epdcx:descriptionSet xmlns:epdcx=\"http://purl.org/eprint/epdcx/2006-11-16/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://purl.org/eprint/epdcx/2006-11-16/ http://www.ukoln.ac.uk/repositories/eprints-application-profile/epdcx/xsd/2006-11-16/epdcx.xsd\">\n");
                    xmlWrite.WriteRaw("<epdcx:description epdcx:resourceId=\"sword-mets-epdcx-1\">\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"lernz.data.category\">\n");
                    xmlWrite.WriteRaw("<epdcx:valueString>Water quality profiles</epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"lernz.data.type\">");
                    xmlWrite.WriteRaw("<epdcx:valueString>" + waterType + "</epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"http://purl.org/dc/elements/1.1/title\">\n");
                    xmlWrite.WriteRaw("<epdcx:valueString>" + Path.GetFileNameWithoutExtension(toSave) + "</epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"lernz.data.provenance\">");
                    xmlWrite.WriteRaw("<epdcx:valueString>SWORD submission</epdcx:valueString>");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"http://purl.org/dc/elements/1.1/creator\">\n");
                    xmlWrite.WriteRaw("<epdcx:valueString>Author/Creator1 of the dataset in the form last name, first name</epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"http://purl.org/dc/elements/1.1/creator\">\n");
                    xmlWrite.WriteRaw("<epdcx:valueString></epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("<epdcx:statement epdcx:propertyURI=\"http://purl.org/dc/terms/abstract\">\n");
                    xmlWrite.WriteRaw("<epdcx:valueString></epdcx:valueString>\n");
                    xmlWrite.WriteRaw("</epdcx:statement>\n");
                    xmlWrite.WriteRaw("</epdcx:description>\n");
                    xmlWrite.WriteRaw("</epdcx:descriptionSet>\n");
                    xmlWrite.WriteEndElement();
                    xmlWrite.WriteRaw("\n</mdWrap>\n");
                    xmlWrite.WriteRaw("</dmdSec>\n");
                    xmlWrite.WriteRaw("\t<fileSec>\n");
                    xmlWrite.WriteRaw("\t\t<fileGrp ID=\"sword-mets-fgrp-1\" USE=\"CONTENT\">\n");
                    xmlWrite.WriteRaw("\t\t\t<file GROUPID=\"sword-mets-fgid-0\" ID=\"sword-mets-file-0\" MIMETYPE=\"text/csv\">\n");
                    xmlWrite.WriteRaw("\t\t\t\t<FLocat LOCTYPE=\"URL\" xlink:href=\"" + Path.GetFileName(toSave) + "\" />");
                    xmlWrite.WriteRaw("\t\t\t</file>\n");
                    xmlWrite.WriteRaw("\t\t\t<file GROUPID=\"sword-mets-fgid-1\" ID=\"sword-mets-file-1\" MIMETYPE=\"application/xml\">\n");
                    xmlWrite.WriteRaw("\t\t\t\t<FLocat LOCTYPE=\"URL\" xlink:href=\"LernzMeta.xml\" />\n");
                    xmlWrite.WriteRaw("\t\t\t</file>\n");
                    xmlWrite.WriteRaw("\t\t</fileGrp>\n");
                    xmlWrite.WriteRaw("\t</fileSec>\n");
                    xmlWrite.WriteRaw("\t<structMap ID=\"sword-mets-struct-1\" LABEL=\"structure\" TYPE=\"LOGICAL\">\n");
                    xmlWrite.WriteRaw("\t\t<div ID=\"sword-mets-div-1\" DMDID=\"sword-mets-dmd-1\" TYPE=\"SWORD Object\">\n");
                    xmlWrite.WriteRaw("\t\t\t<div ID=\"sword-mets-div-2\" TYPE=\"File\">\n");
                    xmlWrite.WriteRaw("\t\t\t\t<fptr FILEID=\"sword-mets-file-0\" />\n");
                    xmlWrite.WriteRaw("\t\t\t\t<fptr FILEID=\"sword-mets-file-1\" />\n");
                    xmlWrite.WriteRaw("\t\t\t</div>\n");
                    xmlWrite.WriteRaw("\t\t</div>\n");
                    xmlWrite.WriteRaw("\t</structMap>\n");
                    xmlWrite.WriteRaw("</mets>\n");

                    xmlWrite.WriteEndDocument();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile("mets.xml");
                zip.AddFile(Path.GetFileName(toSave));
                zip.AddFile("LernzMeta.xml");
                zip.Save(Path.GetFileNameWithoutExtension(fileName) + ".zip");
            }
           
            File.Delete(toSave);
            File.Delete("LernzMeta.xml");
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
            waterType = reader.ReadLine().Substring(11);
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
                firstDepth = false;
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
                            toReturn += "0";
                            toReturn += "\t";
                            counter++;
                            continue;
                        }
                        else
                        {
                            double parsed = double.Parse(splitter[i]);
                            double value = parsed - offset;
                            toReturn += value;
                            toReturn += "\t";
                            counter++;
                            continue;
                        }
                        
                    }

                    if (splitter[i].Equals("0.000e+00"))
                    {
                        continue;
                    }
                    else
                    {
                        if (splitter[i].Contains("e"))
                        {
                            toReturn += Double.Parse(splitter[i], System.Globalization.NumberStyles.Float);
                            toReturn += "\t";
                            continue;

                        }
                        else
                        {
                            toReturn += splitter[i];
                            toReturn += "\t";
                        }
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
                    finalHeader += "TmpWtr";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "TmpWtr";
                    temp.unit = "degC";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Depth"))  //Sort out depth metadata when its all done
                {
                    if (!first)
                    {
                        depthHeader = counter;
                        first = true;
                        finalHeader += "DepthAdj";
                        metaInfo temp = new metaInfo();
                        temp.name = "DepthAdj";
                        temp.unit = "Meters";
                        temp.qc = new qcData();
                        temp.qc.sensorHeight = "0" SBE19plus";
                        finalHeader += "\t";
                        metaData.Add(temp);
                    }
                    else
                    {
                        finalHeader += "Depth";
                        finalHeader += "\t";
                    }
                }
                else if (s.Contains("pH"))
                {
                    finalHeader += "pH";
                    finalHeader += "\t";
                    metaInfo ph = new metaInfo();
                    ph.name = "pH";
                    ph.unit = "";
                    ph.qc = new qcData();
                    ph.qc.sensorHeight = "0.2";
                    ph.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(ph);
                }
                else if (s.Contains("Density"))
                {
                    finalHeader += "Density";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Density";
                    temp.unit = "";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("sbeox0Mg/L"))
                {
                    finalHeader += "DOconc";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "DOconc";
                    temp.unit = "mg/L";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Attenuation"))
                {
                    finalHeader += "BmAtt";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "BmAtt";
                    temp.unit = "1/m";
                  
                    temp.qc = new qcData();
                    temp.qc.model = "Sea-Bird SBE19plus";
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                } 
                else if (s.Contains("sbeox0PS"))
                {
                    finalHeader += "DOsat";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "DOsat";
                    temp.unit = "%sat";

                    temp.qc = new qcData();
                    temp.qc.model = "Sea-Bird SBE19plus";
                    temp.qc.sensorHeight = "0";
                    metaData.Add(temp);
                }
                else if (s.Contains("Conductivity"))
                {
                    finalHeader += "Cond";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Cond_v(mS/cm)";
                    temp.unit = "mS/cm";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Specific Conductance"))
                {
                    finalHeader += "CondSp";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "CondSp";
                    temp.unit = "mS/cm";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Fluorescence"))
                {
                    finalHeader += "FlChl";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "FlChl_v(RFU)";
                    temp.unit = "RFU";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.5";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Beam Transmission"))
                {
                    finalHeader += "BmTran";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "BmTran";
                    temp.unit = "%";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.3";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("PAR/Irradiance"))
                {
                    finalHeader += "RadPAR";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "RadPAR";
                    temp.unit = "umol/m^2/s";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0.7";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else if (s.Contains("Salinity"))
                {
                    finalHeader += "Sal";
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = "Sal";
                    temp.unit = "";
                    temp.qc = new qcData();
                    temp.qc.sensorHeight = "0";
                    temp.qc.model = "Sea-Bird SBE19plus";
                    metaData.Add(temp);
                }
                else
                {
                    finalHeader += s;
                    finalHeader += "\t";
                    metaInfo temp = new metaInfo();
                    temp.name = s;
                    temp.unit = "";
                    metaData.Add(temp);
                }
                counter++;
            }
            finalHeader = finalHeader.TrimEnd(new char[] { '\t' });
            return finalHeader;
        }


    }

    }