namespace SocialnetworkHomework.Workers
{
    public class PostProcessor : BackgroundService
    {
        private RequestActions requestActions ;
        public PostProcessor(RequestActions requestActions) 
        { 
            this.requestActions = requestActions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested ) 
            //{

            //}
        }
    }
}
