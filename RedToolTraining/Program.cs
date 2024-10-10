using System;
using System.Collections.Generic;
using System.Linq;
using ViDi2.Training.Local;
//Must be run on an x64 platform.
//Add NuGet packages from: C:\ProgramData\Cognex\VisionPro Deep Learning\3.3\Examples\packages
//ViDi.NET
//ViDi.NET.VisionPro
namespace RedToolTraining
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Initialize workspace directory
            ViDi2.Training.Local.WorkspaceDirectory workspaceDir = new ViDi2.Training.Local.WorkspaceDirectory();
            //Set the path to workspace directory
            workspaceDir.Path = @"C:\Users\acooper\Desktop\Training";

            //Create a library access instance using the workspace directory
            using (LibraryAccess libraryAccess = new LibraryAccess(workspaceDir))
            {
                //Create a control interface for training tools
                using (ViDi2.Training.IControl myControl = new ViDi2.Training.Local.Control(libraryAccess))
                {
                    //Create a new workspace and add it to the control
                    ViDi2.Training.IWorkspace myWorkspace = myControl.Workspaces.Add("myRedWorkspace");

                    //Add a new stream to the workspace
                    ViDi2.Training.IStream myStream = myWorkspace.Streams.Add("default");

                    //Add a Red Tool to the stream (for defect detection)
                    ViDi2.Training.IRedTool myRedTool = myStream.Tools.Add("Analyze", ViDi2.ToolType.Red) as ViDi2.Training.IRedTool;

                    //Define valid image file extensions
                    List<string> ext = new List<string> { ".jpg", ".bmp", ".png" };

                    //Get all image files in the specified directory that match the extensions
                    IEnumerable<string> imageFiles = System.IO.Directory.GetFiles(
                        @"C:\Users\acooper\Desktop\Training\RedToolTraining\RedToolTraining\Images",
                        "*.*",
                        System.IO.SearchOption.TopDirectoryOnly
                    ).Where(s => ext.Any(e => s.EndsWith(e)));

                    //Add each image to the stream's database
                    foreach (string file in imageFiles)
                    {
                        using (ViDi2.FormsImage image = new ViDi2.FormsImage(file))
                        {
                            myStream.Database.AddImage(image, System.IO.Path.GetFileName(file));
                        }
                    }

                    //Process all images in the Red Tool's database
                    myRedTool.Database.Process();
                    //Wait until the processing is done
                    myRedTool.Wait();

                    //Map of defect for specific image files and set tool parameters
                    myRedTool.Database.LabelViews("'Good'", "");
                    myRedTool.Database.LabelViews("'Bad'", "deviation");
                    myRedTool.Database.SelectTrainingSet("", 0.5);
                    myRedTool.Parameters.NetworkModel = "unsupervised/large";
                    myRedTool.Parameters.ColorChannels = 3;
                    myRedTool.Parameters.FeatureSize = new ViDi2.Size(40, 40);
                    myRedTool.Parameters.Luminance = 0.05;
                    myRedTool.Parameters.Contrast = 0.05;
                    myRedTool.Parameters.CountEpochs = 40;

                    //Start training the Red Tool
                    myRedTool.Train();
                    Console.WriteLine("Starting:");

                    //Monitor the progress of the training
                    while (!myRedTool.Wait(1000))
                    {
                        Console.WriteLine(myRedTool.Progress.Description + " " + myRedTool.Progress.ETA.ToString());
                    }

                    myRedTool.Database.Process();
                    myRedTool.Wait();

                    //Export the runtime workspace to a file
                    using (System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\acooper\Desktop\Training\RedToolRuntime.vrws", System.IO.FileMode.Create))
                    {
                        myWorkspace.ExportRuntimeWorkspace().CopyTo(fs);
                    }

                    //Save the workspace
                    myWorkspace.Save();
                }
            }
        }
    }
}
