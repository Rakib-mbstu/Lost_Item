import axios from "axios";

const api = axios.create({
  baseURL: "/api"
});

api.interceptors.request.use((config) => {
  const authResponse = localStorage.getItem("auth");

  if (authResponse) {
    const parsedAuth = JSON.parse(authResponse);
    console.log("Attaching token to request:", parsedAuth.token);
    config.headers.Authorization = `Bearer ${parsedAuth.token}`;
  }

  return config;
});

export default api;