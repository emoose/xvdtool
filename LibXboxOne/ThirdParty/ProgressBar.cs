using System;
using System.Text;
using System.Threading;

/// <summary>
/// An ASCII progress bar
/// </summary>
namespace LibXboxOne
{
    // Source: https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
    // License: MIT
    public class ProgressBar : IDisposable, IProgress<long>
    {
        private const int blockCount = 30;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private long totalItems = 0;
        private long currentItem = 0;
        private string description = String.Empty;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar(long items, string unitDescription="") {
            totalItems = items;
            description = unitDescription;
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected) {
                ResetTimer();
            }
        }

        public void Report(long current) {
            Interlocked.Exchange(ref currentItem, current);
        }

        private void TimerHandler(object state) {
            lock (timer) {
                if (disposed) return;

                double progress = (double)(currentItem + 1) / totalItems;
                int progressBlockCount = (int) (progress * blockCount);
                int percent = (int) (progress * 100);
                string text = string.Format("{0,3}% [{1}{2}] {3}/{4} {5} {6}",
                    percent,
                    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                    currentItem,
                    totalItems,
                    description,
                    animation[animationIndex++ % animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text) {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength]) {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0) {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer() {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose() {
            lock (timer) {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}