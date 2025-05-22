namespace FileAnalisysService.Models;

public class Similarity
{
    public Guid OtherFileId { get; set; }
    public double Score { get; set; }
}