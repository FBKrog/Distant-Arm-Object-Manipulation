using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ResearchLogging
{
    public class DataLogger : MonoBehaviour
    {
        [Header("Log info")]
        [SerializeField] private string fileName = "TaskTimings";
        [SerializeField] private string folderName = "Logs";

        private List<DaomLogData> daomLogData = new List<DaomLogData>();

        public static DataLogger Instance { get; protected set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log($"DataLogger {Instance.name} already exists, destroying the one attached to {name}", this);
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(Instance);
        }

        public void TaskUpdate(string taskName)
        {
            daomLogData.Add(new DaomLogData(taskName, Time.time));
            LSLMarkerSender.Instance.SendMarker(taskName);
        }

        /// <summary>
        /// Saves the log as a csv.
        /// To be called at scene end.
        /// </summary>
        public void SaveLogAsCsv()
        {
            //Create file
            int logNum = 0;

            string myDataPath = Application.persistentDataPath;
            // Go from last to first character
            for (int i = myDataPath.Length - 1; i >= 0; i--)
            {
                // Remove last / or \ in the filepath
                if (myDataPath[i] == '/' || myDataPath[i] == '\\')
                {
                    myDataPath = myDataPath.Remove(i);
                    break;
                }
            }

            // Create folder if it doesn't already exist
            // CreateDirectory is smartypants and doesn't create something that already exists
            string folderPath = Path.Combine(myDataPath, folderName);
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName + "_" + logNum + ".csv");

            // If file already exists, count up
            while (File.Exists(filePath))
            {
                logNum++;
                filePath = Path.Combine(folderPath, fileName + "_" + logNum + ".csv");
            }

            Debug.Log($"Logging at {filePath}. {daomLogData.Count} log entries.");

            // Create text element
            StreamWriter writer = File.CreateText(filePath);

            // Write titles
            writer.WriteLine("Task;Time");

            // Write out the data
            for (int i = 0; i < daomLogData.Count; i++)
            {
                writer.WriteLine($"{daomLogData[i].name};{daomLogData[i].time.ToString("0.00")}");
            }

            // Close the writer
            writer.Close();
        }
    }

    public struct DaomLogData
    {
        public string name;
        public float time;
        public DaomLogData(string name, float time)
        {
            this.name = name;
            this.time = time;
        }
    }
}