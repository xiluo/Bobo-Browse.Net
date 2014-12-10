namespace BoboBrowse.Net.Facets.Statistics
{
    public class ChiSquaredFacetCountStatisticsGenerator : FacetCountStatisicsGenerator
    {
        public override double calculateDistributionScore(int[] distribution, int collectedSampleCount, int numSamplesCollected, int totalSamplesCount)
        {
            double expected = (double)collectedSampleCount / (double)numSamplesCollected;

            double sum = 0.0;
            foreach (int count in distribution)
            {
                double v = (double)count - expected;
                sum += (v * v);
            }

            return sum / expected;
        }
    }
}