import type { MetaFunction } from "@remix-run/node";
import { useEffect, useRef, useState } from "react";
import logoS from "./logo-s.png";

export const meta: MetaFunction = () => {
  return [
    { title: "New Remix App" },
    { name: "description", content: "Welcome to Remix!" },
  ];
};

export default function Index() {
  const [messages, setMessages] = useState<string[]>([]);
  const initialized = useRef(false)

  useEffect(() => {
    if (!initialized.current) {
      console.log("init");
      initialized.current = true

      const socket = new WebSocket('ws://localhost:8000/ws');
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
      <div className="px-16">
        <img src="./logo-s.png" alt="image description">
        </img>
        <p className="h-10 text-green-900 font-headline tracking-tight font-extrabold">Azure Communication Service</p>
        <hr className="w-3/5" />
        <h1
          className="mt-6 text-5xl font-headline tracking-tight text-gray-900 leading-snug"
          role="heading"
          aria-level={1}
        >
          We got your plants. <br />
          <span className="text-green-700" role="heading" aria-level={1}
          >And we deliver them for you.</span>
        </h1>
        <p className="w-3/5 mt-2 text-gray-600 text-lg" aria-level={2}>
          Our hand-picked collection of plants gives you all the natural wonders
          you ever wanted in your room, living space or even kitchen.
        </p>
        <div className="mt-8 flex" role="button">
          <a
            className="flex items-center justify-center px-8 py-3 font-medium rounded-md text-white bg-green-700 shadow uppercase hover:bg-green-800 hover:shadow-lg transform transition hover:-translate-y-1 focus:ring-2 focus:ring-green-600 ring-offset-2 outline-none focus:bg-green-800 focus:shadow-lg active:bg-green-900"
            href="#"
          >See the collection</a>
          <a
            className="flex items-center justify-center px-8 py-3 ml-4 font-medium rounded-md text-green-700 bg-white shadow uppercase hover:shadow-lg transform transition hover:-translate-y-1 focus:ring-2 focus:ring-green-600 ring-offset-2 outline-none focus:shadow-lg"
            href="#"
          >Learn more</a>
        </div>
      </div>
      <div className="mr-40" role="img">
        <img
          className="object-cover object-center w-96 rounded-md hover:shadow-lg transform transition hover:-translate-y-2"
          src="https://images.pexels.com/photos/3952029/pexels-photo-3952029.jpeg"
          alt="Image of plants"
        />
      </div>
    </div>
  );
}
