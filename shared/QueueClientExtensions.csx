#r "Microsoft.ServiceBus"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.ServiceBus.Messaging;



private const long MaxBathSizeInBytes = 262144;

#region Private Constants 
//******************************* 
// Formats 
//******************************* 
const string BrokeredMessageListCannotBeNullOrEmpty = "The brokeredMessageEnumerable parameter cannot be null or empty.";
const string SendPartitionedBatchFormat = "[QueueClient.SendPartitionedBatch] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
const string SendPartitionedBatchAsyncFormat = "[QueueClient.SendPartitionedBatchAsync] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
#endregion

#region Public Methods 
/// <summary> 
/// Sends a set of brokered messages (for batch processing).  
/// If the batch size is greater than the maximum batch size,  
/// the method partitions the original batch into multiple batches,  
/// each smaller in size than the maximum batch size. 
/// </summary> 
/// <param name="queueClient">The current QueueClient object.</param> 
/// <param name="brokeredMessageEnumerable">The collection of brokered messages to send.</param> 
/// <param name="trace">true to cause a message to be written; otherwise, false.</param> 
/// <returns>The asynchronous operation.</returns> 
public async static Task SendPartitionedBatchAsync(this QueueClient queueClient, IEnumerable<BrokeredMessage> brokeredMessageEnumerable, bool trace = false)
{
    var brokeredMessageList = brokeredMessageEnumerable as IList<BrokeredMessage> ?? brokeredMessageEnumerable.ToList();
    if (brokeredMessageEnumerable == null || !brokeredMessageList.Any())
    {
        throw new ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
    }

    var batchList = new List<BrokeredMessage>();
    long batchSize = 0;

    foreach (var brokeredMessage in brokeredMessageList)
    {
        if ((batchSize + brokeredMessage.Size) > MaxBathSizeInBytes)
        {
            // Send current batch 
            await queueClient.SendBatchAsync(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));

            // Initialize a new batch 
            batchList = new List<BrokeredMessage> { brokeredMessage };
            batchSize = brokeredMessage.Size;
        }
        else
        {
            // Add the BrokeredMessage to the current batch 
            batchList.Add(brokeredMessage);
            batchSize += brokeredMessage.Size;
        }
    }
    // The final batch is sent outside of the loop 
    await queueClient.SendBatchAsync(batchList);
    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));
}

/// <summary> 
/// Sends a set of brokered messages (for batch processing).  
/// If the batch size is greater than the maximum batch size,  
/// the method partitions the original batch into multiple batches,  
/// each smaller in size than the maximum batch size. 
/// </summary> 
/// <param name="queueClient">The current QueueClient object.</param> 
/// <param name="brokeredMessageEnumerable">The collection of brokered messages to send.</param> 
/// <param name="trace">true to cause a message to be written; otherwise, false.</param> 
public static void SendPartitionedBatch(this QueueClient queueClient, IEnumerable<BrokeredMessage> brokeredMessageEnumerable, bool trace = false)
{
    var brokeredMessageList = brokeredMessageEnumerable as IList<BrokeredMessage> ?? brokeredMessageEnumerable.ToList();
    if (brokeredMessageEnumerable == null || !brokeredMessageList.Any())
    {
        throw new ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
    }

    var batchList = new List<BrokeredMessage>();
    long batchSize = 0;

    foreach (var brokeredMessage in brokeredMessageList)
    {
        if ((batchSize + brokeredMessage.Size) > MaxBathSizeInBytes)
        {
            // Send current batch 
            queueClient.SendBatch(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));

            // Initialize a new batch 
            batchList = new List<BrokeredMessage> { brokeredMessage };
            batchSize = brokeredMessage.Size;
        }
        else
        {
            // Add the BrokeredMessage to the current batch 
            batchList.Add(brokeredMessage);
            batchSize += brokeredMessage.Size;
        }
    }
    // The final batch is sent outside of the loop 
    queueClient.SendBatch(batchList);
    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));
}
#endregion
