import type { MetaFunction } from "@remix-run/node";
import { useEffect, useState } from "react";

export const meta: MetaFunction = () => {
  return [
    { title: "New Remix App" },
    { name: "description", content: "Welcome to Remix!" },
  ];
};

export default function Index() {
  const [messages, setMessages] = useState<string[]>([]);
  useEffect(() => {
    const socket = new WebSocket('https://echo.websocket.org/.ws');
    // Connection opened
    socket.addEventListener("open", (event) => {
      socket.send("Hello Server!");
    });
    // Listen for messages
    socket.addEventListener("message", (event) => {
      setMessages((prev) => [...prev, event.data]);
    });
  }, [messages]);
  return (
    <div className="flex h-screen items-center justify-center">
      <div className="flex flex-col items-center gap-16">
        <header className="flex flex-col items-center gap-9">
          <h1 className="leading text-2xl font-bold text-gray-800 dark:text-gray-100">
            Azure Communication Services
          </h1>
          <div className="h-[144px] w-[434px]">
            <img
              src="/logo-light.png"
              alt="Remix"
              className="block w-full dark:hidden"
            />
            <img
              src="/logo-dark.png"
              alt="Remix"
              className="hidden w-full dark:block"
            />
          </div>
          <div>
            {messages && <div>{messages}</div>}
          </div>
        </header>
      </div>
    </div>
  );
}
