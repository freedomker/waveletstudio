﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Qios.DevSuite.Components;
using Qios.DevSuite.Components.Ribbon;
using WaveletStudio.MainApplication.Controls;
using WaveletStudio.ProcessingSteps;
using WaveletStudio.SignalGeneration;
using ZedGraph;

namespace WaveletStudio.MainApplication.Forms
{
    public partial class MainForm : QRibbonForm
    {

        public MainForm()
        {
            InitializeComponent();
            OriginalSignalGraph.GraphPane.IsFontsScaled = false;
            CreatedSignalGraph.GraphPane.IsFontsScaled = false;
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            LoadRibbon();
        }

        private void LoadRibbon()
        {
            LoadSignalTemplates();
        }

        private void LoadSignalTemplates()
        {            
            foreach (var type in Utils.GetTypes("WaveletStudio.SignalGeneration"))
            {
                var signal = (CommonSignalBase)Activator.CreateInstance(type);
                var item = QControlUtils.CreateCompositeListItem(
                                                                    signal.Name,
                                                                    signal.Name.ToLower(),
                                                                    signal.Name,
                                                                    "",
                                                                    1,
                                                                    QPartDirection.Vertical,
                                                                    QPartAlignment.Centered,
                                                                    Color.White);
                item.ItemActivated += SignalTemplateSelected;
                SignalTemplatesComposite.Items.Add(item);
            }            
        }

        private SignalGenerationForm _signalGenerationForm;

        public ProcessingStepList ProcessingSteps = new ProcessingStepList();

        public void SignalTemplateSelected(object sender, QCompositeEventArgs args)
        {
            var templateName = ((QCompositeItem) sender).ItemName;
            var firstTime = _signalGenerationForm == null;

            if (firstTime)
            {
                ShowSignalGenerationForm(templateName);
                return;
            }
            _signalGenerationForm.SignalTemplateName = templateName;
            _signalGenerationForm.Run();
            SelectSignalGenerationTemplate(_signalGenerationForm.SignalTemplateName);
        }

        private void ShowSignalGenerationForm(string templateName)
        {
            if (_signalGenerationForm == null)            
                _signalGenerationForm = new SignalGenerationForm();
            if (!string.IsNullOrEmpty(templateName))
                _signalGenerationForm.SignalTemplateName = templateName;
            _signalGenerationForm.ShowDialog();
            if (_signalGenerationForm.GeneratedSignal == null || _signalGenerationForm.DialogResult != DialogResult.OK)
                return;
            SelectSignalGenerationTemplate(_signalGenerationForm.SignalTemplateName);
        }

        private void SelectSignalGenerationTemplate(string templateName)
        {
            foreach (var item in SignalTemplatesComposite.Items)
            {
                var compositeItem = (QCompositeItem) item;
                compositeItem.Checked = compositeItem.ItemName == templateName;
            }

            var step = ProcessingSteps.GetStep(GenerateSignalStep.StepKey) as GenerateSignalStep;
            if (step == null)
            {
                ProcessingSteps.RemoveAll(it => it.ProcessingType == ProcessingStepBase.ProcessingTypeEnum.CreateSignal);
                ProcessingSteps.Insert(0, step = new GenerateSignalStep());
            }
            step.Template = _signalGenerationForm.Template;
            UpdateForm();
        }

        private void SignalTemplatePanelCaptionShowDialogItemActivated(object sender, QCompositeEventArgs e)
        {
            ShowSignalGenerationForm(null);
        }

        private void UpdateGraph(Signal signal, ZedGraphControl graph)
        {
            var pane = graph.GraphPane;
            if (signal == null || signal.Samples == null)
            {
                pane.CurveList.Clear();
                return;
            }
            
            var title = "";
            if (graph == OriginalSignalGraph)
                title = "Original Signal";
            else if (graph == OriginalSignalGraph)
                title = "Output";

            var samples = signal.GetSamplesPair();            
            
            if (pane.CurveList.Count > 0)
                pane.CurveList.RemoveAt(0);
            var yAxys = new PointPairList();
            yAxys.AddRange(samples.Select(it => new PointPair(it[1], it[0])));
            pane.AddCurve("", yAxys, Color.Red, SymbolType.None);
            pane.Legend.IsVisible = false;
            pane.Title.IsVisible = title != "";
            pane.Title.Text = title;
            pane.Title.FontSpec = new FontSpec("Arial", 11, Color.Black, true, false, false);
            pane.XAxis.Title.IsVisible = false;
            pane.YAxis.Title.IsVisible = false;
            if (!pane.IsZoomed && samples != null)
            {
                pane.XAxis.Scale.Min = samples.ElementAt(0)[1];
                pane.XAxis.Scale.Max = samples.ElementAt(samples.Count() - 1)[1];
            }
            graph.AxisChange();
            graph.Invalidate();
            graph.Refresh();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            var step = new ScalarStep {Operation = ScalarStep.OperationEnum.Multiply, Scalar = 2.1};
            ProcessingSteps.RemoveAll(it => it.Key == ScalarStep.StepKey);
            ProcessingSteps.Add(step);
            UpdateForm();
        }

        private void UpdateForm()
        {
            ProcessingSteps.Process();
            UpdateProcessingStepsList();

            var firstStep = ProcessingSteps.FirstOrDefault();
            if (firstStep != null)
                UpdateGraph(firstStep.Signal, OriginalSignalGraph);
            else
                OriginalSignalGraph.GraphPane.CurveList.Clear();
            
            var lastStep = ProcessingSteps.LastOrDefault(it => it.ProcessingType != ProcessingStepBase.ProcessingTypeEnum.CreateSignal);
            if (lastStep != null)
                UpdateGraph(lastStep.Signal, CreatedSignalGraph);
            else
                CreatedSignalGraph.GraphPane.CurveList.Clear();
        }

        public void UpdateProcessingStepsList()
        {
            StepsComposite.Items.Clear();
            foreach (var step in ProcessingSteps)
            {
                var item = QControlUtils.CreateCompositeListItem(
                                                                step.Id.ToString(),
                                                                step.Name.ToLower().Replace(" ", ""),
                                                                step.Name,
                                                                step.Description.Replace(";", "\r\n"),
                                                                1,
                                                                QPartDirection.Horizontal,
                                                                QPartAlignment.Near,
                                                                Color.White, 60, 45);
                item.ItemActivated += StepSelected;
                StepsComposite.Items.Add(item);
            }
        }

        private void ShowOriginalSignalCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            SplitContainer.Panel1Collapsed = !ShowOriginalSignalCheckBox.Checked;
            SplitContainer.Panel2Collapsed = !ShowCreatedSignal.Checked;
            if (!ShowOriginalSignalCheckBox.Checked && !ShowCreatedSignal.Checked && sender == ShowOriginalSignalCheckBox)
            {
                ShowCreatedSignal.Checked = true;
            }
            else if (!ShowOriginalSignalCheckBox.Checked && !ShowCreatedSignal.Checked && sender == ShowCreatedSignal)
            {
                ShowOriginalSignalCheckBox.Checked = true;
            }
        }

        public void StepSelected(object sender, QCompositeEventArgs args)
        {
            var stepName = ((QCompositeItem)sender).ItemName;
            MessageBox.Show(stepName);
        }
    }
}

