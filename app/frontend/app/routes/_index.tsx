import type { MetaFunction } from "@remix-run/node";
import { useEffect, useRef, useState } from "react";

export const meta: MetaFunction = () => {
  return [
    { title: "New Remix App" },
    { name: "description", content: "Welcome to Remix!" },
  ];
};

interface ApiData {
  userId: number;
  id: number;
  title: string;
  body: string;
}

// export const getApiData = async () : Promise<ApiData> => {
//   try {
//     const getRequestOptions = {
//       method: 'GET'
//     };
//     const response = await fetch(`${import.meta.env.VITE_BASE_URL}/api/agent/info`, getRequestOptions);
//     if (!response.ok) {
//       throw new Error(`Failed to to get agent page data. Status: ${response.status}`);
//     }
//     const details = await response.json().then((data) => data);
//     return details;
//   } catch (error) {
//     console.error('Failed to get agent page data. Error: ', error);
//     throw new Error('Failed to get agent page data');
//   }
// }

export default function Index() {
  const [messages, setMessages] = useState<string[]>([]);
  const initialized = useRef(false)

  useEffect(() => {
    if (!initialized.current) {
      console.log("init");
      initialized.current = true
      console.log(import.meta.env.VITE_WS_URL)
      const socket = new WebSocket(import.meta.env.VITE_WS_URL);
      // Connection opened
      socket.addEventListener("open", (event) => { });
      // Listen for messages
      socket.addEventListener("message", (event) => {
        // Add message to set of messages
        setMessages((prevMessages) => [...prevMessages, event.data]);
      });
    }
  }, []);
  return (
    <div className="m-16 flex justify-center items-center" role="main">
      <div className="px-6">
        <div className="flex gap-4 ">
          <div className="content-center">
            <img src="./logo.png" alt="image description" />
          </div>
          <div>
            <div>
              <p
                className="text-2xl font-headline tracking-tight text-gray-900">
                Contoso Transcript Portal
              </p>
              <p
                className="text-sm font-headline tracking-tight text-gray-900">
                Azure Communication Services
              </p>
            </div>
          </div>
        </div>
        <hr className="w-3/5 mt-5" />
        <div className="w-5/5 mt-5 text-gray-600 text-sm" aria-level={2}>
          <ul>
            {messages.map((message, index) => (
              <li key={index}>{message}</li>
            ))}
          </ul>
        </div>
      </div>
    </div >
  );
}
