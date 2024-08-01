## What is ChatSummary?
ChatSummary is a powerful C# application designed to extract, analyze, and summarize messages from Matrix chat rooms. It uses advanced AI models to distill lengthy conversations into digestible summaries, helping you stay informed without the information overload.

## Getting Started
To use ChatSummary, you'll need to:
1. Clone the repository and build the project.
2. Configure your `settings.json` file with your Matrix credentials and preferred AI service.
3. Run the application to start summarizing your chats!

## How It Works
1. ChatSummary connects to your specified Matrix room and retrieves recent messages.
2. It groups these messages into manageable chunks based on time and message count.
3. The grouped messages are then sent to the configured AI service for analysis.
4. The AI generates a concise summary, highlighting the most important points and takeaways.
5. Finally, the summary is posted back to a designated Matrix room for easy access.
