﻿using System;
using System.Collections.Generic;
using System.IO;
using WaveletStudio.Blocks.CustomAttributes;

namespace WaveletStudio.Blocks
{
    /// <summary>
    /// Generates a signal based on a CSV file
    /// </summary>
    [SingleInputOutputBlock]
    [Serializable]
    public class SignalFromCSVBlock : BlockBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SignalFromCSVBlock()
        {
            BlockBase root = this;
            CreateNodes(ref root);
            FilePath = "example.csv";
            ColumnSeparator = ",";
            SignalStart = 0;
            SamplingInterval = 1;
        }

        /// <summary>
        /// Name
        /// </summary>
        public override string Name { get { return "CSV Signal"; } }

        /// <summary>
        /// Description
        /// </summary>
        public override string Description { get { return "Generates a signal based on a CSV file"; } }

        /// <summary>
        /// Processing type
        /// </summary>
        public override ProcessingTypeEnum ProcessingType { get { return ProcessingTypeEnum.LoadSignal; } }

        [Parameter]
        public string FilePath { get; set; }

        [Parameter]
        public string ColumnSeparator { get; set; }

        [Parameter]
        public int SignalStart { get; set; }

        private int _samplingRate;

        private double _samplingInterval;

        [Parameter]
        public double SamplingInterval 
        {
            get { return _samplingInterval; }
            set
            {
                _samplingInterval = value;
                if (Math.Abs(value - 0d) > double.Epsilon)
                {
                    _samplingRate = Convert.ToInt32(Math.Round(1 / value));   
                }
            }
        }

        [Parameter]
        public bool IgnoreFirstRow { get; set; }

        [Parameter]
        public bool SignalNameInFirstColumn { get; set; }

        /// <summary>
        /// Executes the block
        /// </summary>
        public override void Execute()
        {
            OutputNodes[0].Object.Clear();
            var filePath = FilePath;
            if(!Path.IsPathRooted(filePath))
                filePath = Path.Combine(Utils.AssemblyDirectory, filePath);
            if(!File.Exists(filePath))
                return;

            var lineNumber = 0;
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                lineNumber++;
                if (lineNumber == 1 && IgnoreFirstRow)
                    continue;
                var signal = ParseLine(line);
                if (signal != null)
                {
                    if (signal.Name == "")
                        signal.Name = "Line " + lineNumber;
                    OutputNodes[0].Object.Add(signal);   
                }                
            }
            if (Cascade && OutputNodes[0].ConnectingNode != null)
                OutputNodes[0].ConnectingNode.Root.Execute();            
        }

        private Signal ParseLine(string line)
        {
            if(string.IsNullOrWhiteSpace(line))
                return null;

            var values = new List<double>();
            var samples = line.Split(new[] {ColumnSeparator}, StringSplitOptions.RemoveEmptyEntries);
            var columnNumber = 0;
            var signalName = "";
            foreach (var sampleString in samples)
            {
                columnNumber++;
                if (columnNumber == 1 && SignalNameInFirstColumn)
                {
                    signalName = sampleString;
                    continue;
                }
                double value;
                if (double.TryParse(sampleString, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value))
                    values.Add(value);
            }
            if(values.Count == 0)
                return null;

            return new Signal(values.ToArray())
                             {
                                 Name = signalName,
                                 Start = SignalStart,
                                 SamplingRate = _samplingRate,
                                 SamplingInterval = SamplingInterval,
                                 Finish = SignalStart + SamplingInterval*values.Count - SamplingInterval
                             };            
        }

        protected override sealed void CreateNodes(ref BlockBase root)
        {
            root.OutputNodes = new List<BlockOutputNode> {new BlockOutputNode(ref root, "Signal", "S")};
        }


        /// <summary>
        /// Clone the block, including the template
        /// </summary>
        /// <returns></returns>
        public override BlockBase Clone()
        {
            var block = (SignalFromCSVBlock)MemberwiseClone();
            block.Execute();
            return block;
        }

        /// <summary>
        /// Clones this block but mantains the links
        /// </summary>
        /// <returns></returns>
        public override BlockBase CloneWithLinks()
        {
            var block = (SignalFromCSVBlock)MemberwiseCloneWithLinks();
            block.Execute();
            return block;
        }
    }
}
