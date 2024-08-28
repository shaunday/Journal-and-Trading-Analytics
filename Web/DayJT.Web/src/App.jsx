import "./App.css";
import { useTrades } from "@hooks/useTrades";
import JournalContainer from "@views/JournalContainer";

function App() {
  const { trades } = useTrades();

  //todo how to monitor status for multiple?
  if (trades.status === "pending") {
    return <span>Loading...</span>;
  }

  if (trades.status === "error") {
    return <span>Error: {allTradesQuery.error.message}</span>;
  }

  return (
    <div id="vwrapper">
      <div id="header">Header placeholder</div>
      <div id="mainBody">
        <div className="flexChildCenter gotRightSideNeighbour">
          Metrics placeholder
        </div>
        <JournalContainer trades={allTradesQuery.data} />
      </div>
      <div id="footer">Footer placeholder</div>
    </div>
  );
}

export default App;
