import axios from "axios";
import { clearToken, getToken, isTokenExpired } from "./auth";

const request = axios.create({
    baseURL:"/api",
    timeout: 30000
})

request.interceptors.request.use(config =>{
    const token = getToken()
    if(token){
        if(isTokenExpired(token)){
            clearToken()
            window.location.href = "/login"
            return Promise.reject(new Error("登录已过期"))
        }
        config.headers.Authorization = `Bearer ${token}`
    }
    return config    
},error =>{
    return Promise.reject(error)
})

request.interceptors.response.use(response =>{
    return response
}, error =>{
    if(error.response?.status === 401){
        clearToken()
        window.location.href = "/login"
    }
    return Promise.reject(error)
})

export default request;
