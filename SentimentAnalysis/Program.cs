// <Snippet1>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ML.Legacy;
using Microsoft.ML.Legacy.Models;
using Microsoft.ML.Legacy.Data;
using Microsoft.ML.Legacy.Transforms;
using Microsoft.ML.Legacy.Trainers;
using Microsoft.ML.Runtime.Api;
// </Snippet1>

namespace SentimentAnalysis
{
    class Program
    {
        // <Snippet2>
        static readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "sentiment-braf-train.txt");
        static readonly string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "sentiment-braf-test.txt");
        static readonly string _modelpath = Path.Combine(Environment.CurrentDirectory, "Data", "Model.zip");
        // </Snippet2>

        static async Task Main(string[] args)
        {
            // <Snippet3>
            var model = await Train();
            // </Snippet3>

            // <Snippet12>
            Evaluate(model);
            // </Snippet12>
            
            // <Snippet17>
            Predict(model);
         
            
            
            
            // </Snippet17>


            Console.Read();
        }



        private static string CleanText(string text)
        {

            // 1. Remove URL's
            //string urlPattern = @"https?:\/\/\S+\b|www\.(\w +\.)+\S*";
            //Regex rgx = new Regex(urlPattern);
            //text = rgx.Replace(text, "");

            //// 2. Remove Twitter ID's string userIDPattern = @"@\w+"; rgx = new Regex(userIDPattern); 
            //text = rgx.Replace(text, "");
            //// 3. Remove Numbers 
            //string numberPattern = @"[-+]?[.\d]*[\d]+[:,.\d]*"; text = Regex.Replace(text, numberPattern, "");
            //// 4. Replace Hashtag 
            //string hashtagPattern = @"#";
            //text = Regex.Replace(text, hashtagPattern, "");


            text = Regex.Replace(text, @"http[^\s]+", "");
            text = Regex.Replace(text, @"braf", "", RegexOptions.IgnoreCase);

            text = Regex.Replace(text, @"(@[A-Za-z0-9_]+)", "");
            text = Regex.Replace(text, @"(^|\\s)#(\\w*[a-zA-Z_]+\\w*)", "");
            text = text.Replace("\"", "");
            text = text.Replace("rt ", "", StringComparison.InvariantCultureIgnoreCase);
            text = text.Replace("Twitter", "", StringComparison.InvariantCultureIgnoreCase);
            text = text.Replace("facebook", "", StringComparison.InvariantCultureIgnoreCase);
            text = text.Replace("@", "", StringComparison.InvariantCultureIgnoreCase);
            text = text.Replace(":", "", StringComparison.InvariantCultureIgnoreCase);






            return text;
        }


        public static async Task<PredictionModel<SentimentData, SentimentPrediction>> Train()
        {


            string text = File.ReadAllText(_dataPath).ToLower();
            text = CleanText(text);
            //text =  Regex.Replace(text, @"[^\w\- ]", "",
            //    RegexOptions.None, TimeSpan.FromSeconds(1.5));

            File.WriteAllText(_dataPath, text);
            text = File.ReadAllText(_testDataPath).ToLower();
            text = CleanText(text);


            //text = Regex.Replace(text, @"[^\w\- ]", "",
            //    RegexOptions.None, TimeSpan.FromSeconds(1.5));


            File.WriteAllText(_testDataPath, text);





            // LearningPipeline allows you to add steps in order to keep everything together 
            // during the learning process.  
            // <Snippet5>
            var pipeline = new LearningPipeline();
            // </Snippet5>

            // The TextLoader loads a dataset with comments and corresponding postive or negative sentiment. 
            // When you create a loader, you specify the schema by passing a class to the loader containing
            // all the column names and their types. This is used to create the model, and train it. 
            // <Snippet6>
            pipeline.Add(new TextLoader(_dataPath).CreateFrom<SentimentData>());
            // </Snippet6>

            // TextFeaturizer is a transform that is used to featurize an input column. 
            // This is used to format and clean the data.
            // <Snippet7>
            pipeline.Add(new TextFeaturizer("Features", "SentimentText"));
            //</Snippet7>

            // Adds a FastTreeBinaryClassifier, the decision tree learner for this project, and 
            // three hyperparameters to be used for tuning decision tree performance.
            // <Snippet8>
            pipeline.Add(new FastTreeBinaryClassifier() { NumLeaves = 50, NumTrees = 50, MinDocumentsInLeafs = 20 });
            // </Snippet8>

            // Train the pipeline based on the dataset that has been loaded, transformed.
            // <Snippet9>
            PredictionModel<SentimentData, SentimentPrediction> model =
                pipeline.Train<SentimentData, SentimentPrediction>();
            // </Snippet9>

            // Saves the model we trained to a zip file.
            // <Snippet10>
            await model.WriteAsync(_modelpath);
            // </Snippet10>

            // Returns the model we trained to use for evaluation.
            // <Snippet11>
            return model;
            // </Snippet11>
        }

        public static void Evaluate(PredictionModel<SentimentData, SentimentPrediction> model)
        {
            // Evaluates.
            // <Snippet13>
            var testData = new TextLoader(_testDataPath).CreateFrom<SentimentData>();
            // </Snippet13>

            // BinaryClassificationEvaluator computes the quality metrics for the PredictionModel
            // using the specified data set.
            // <Snippet14>
            var evaluator = new BinaryClassificationEvaluator();
            // </Snippet14>

            // BinaryClassificationMetrics contains the overall metrics computed by binary classification evaluators.
            // <Snippet15>
            BinaryClassificationMetrics metrics = evaluator.Evaluate(model, testData);
            // </Snippet15>

            // The Accuracy metric gets the accuracy of a classifier, which is the proportion 
            // of correct predictions in the test set.

            // The Auc metric gets the area under the ROC curve.
            // The area under the ROC curve is equal to the probability that the classifier ranks
            // a randomly chosen positive instance higher than a randomly chosen negative one
            // (assuming 'positive' ranks higher than 'negative').

            // The F1Score metric gets the classifier's F1 score.
            // The F1 score is the harmonic mean of precision and recall:
            //  2 * precision * recall / (precision + recall).

            // <Snippet16>
            Console.WriteLine();
            Console.WriteLine("PredictionModel quality metrics evaluation");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"Auc: {metrics.Auc:P2}");
            Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
            // </Snippet16>
        }

        public static void Predict(PredictionModel<SentimentData, SentimentPrediction> model)
        {
            // Adds some comments to test the trained model's predictions.
            // <Snippet18>
            IEnumerable<SentimentData> sentiments = new[]
            {
                //new SentimentData
                //{
                //    SentimentText = "Please refrain from adding nonsense to Wikipedia."
                //},
                //new SentimentData
                //{
                //    SentimentText = "He is the best, and the article should say that."
                //}


                 new SentimentData
            {
                SentimentText = "p90RSK blockade inhibits dual BRAF and MEK inhibitor-resistant melanoma by targeting protein synthesis. https://t.co/7wqK5zF7AZ.",
                Sentiment = 1
            },
            new SentimentData
            {
                SentimentText = "RT @PCGddCdd: Braf croesawu Leanne Wood i Ogledd Caerdydd. Great to have the support of Leanne Wood in Cardiff North  Vote... https://t.co/�",
                Sentiment = 0
            },
            new SentimentData
            {
                SentimentText = "RT @emhlung: Promising news of a new target in a subset of lung cancer: BRAF V600-mutant adenocarcinoma https://t.co/ng6NPlyb6F via @Cancer�..",
                Sentiment = 1
            }
            ,
            new SentimentData
            {
            SentimentText = "RT @CTywydd: Tywydd I nifer heddiw! Cofiwch wylio ar @S4C a dilynwch ein cyfrifon Twitter ,facebook ac Instagram / Not too bad today",
            Sentiment = 0
            },


            new SentimentData
            {
                SentimentText = " Combinations of inhibitor and anti-PD-1/PD-L1 antibody improve survival and tumour immunity in an immunocompetent model of orthotopic murine anaplastic thyroid cancer. ",
                Sentiment = 1
            },



            new SentimentData
            {
                SentimentText = "@HeleddAnj wherever you choose Heledd, make sure you are not too far away from facilities, yn enwedig gydar un bach..syniad braf #Adventure ",
                Sentiment = 0
            },
            new SentimentData
            {
                SentimentText = "Hi there, we are aware of this video and have reported this matter to Essex Police. As this is now a police matter, we cannot comment further. ",
                Sentiment = -0
            },


            new SentimentData
            {
                SentimentText = "Differential Radiographic Appearance of BRAF V600E�Mutant met CRC [12/2016] Atreya et al. @JNCCN  https://t.co/dqNWTQgH6z #crcsm #oncorad",
                Sentiment = 1
            },

            new SentimentData
            {
                SentimentText =   "@pmarignani The cool part is that MYC can actually reduce Oncogene induced senescence (OIS) by other oncogenes like BRAF &amp; NRAS: https://t.co/tjyXpNng0g",
                Sentiment = 1
            },

            new SentimentData
            {
                SentimentText =   "RT @ColinBarker75: Bore braf yn Belgrade. Lovely morning in Belgrade #walesaway #togetherstronger #wales #belgrade #cymru https://t.co/bdI2�",
                Sentiment = 0
            },



            };
            // </Snippet18>

            // Use the model to predict the positive 
            // or negative sentiment of the comment data.
            // <Snippet19>
            IEnumerable<SentimentPrediction> predictions = model.Predict(sentiments);
            // </Snippet19>

            // <Snippet20>
            Console.WriteLine();
            Console.WriteLine("Sentiment Predictions");
            Console.WriteLine("---------------------");
            // </Snippet20>

            // Builds pairs of (sentiment, prediction)
            // <Snippet21>
            var sentimentsAndPredictions = sentiments.Zip(predictions, (sentiment, prediction) => (sentiment, prediction));
            // </Snippet21>

            // <Snippet22>
            foreach (var item in sentimentsAndPredictions)
            {
                Console.WriteLine($"Sentiment: {item.sentiment.SentimentText} | Prediction: {(item.prediction.Sentiment ? "Positive" : "Negative")}");
            }
            Console.WriteLine();
            // </Snippet22>          
        }
    }
}
