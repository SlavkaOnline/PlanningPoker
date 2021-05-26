import axios, { AxiosRequestConfig, AxiosPromise, AxiosResponse } from 'axios';
import {User} from "./models";

axios.interceptors.response.use((response) => {

    return response;
}, (error) => {
    if (error.response.status === 401) {
        window.location.href = error.response.headers.location ;
    }
});



export async function login(name:string): Promise<User> {
    return axios.post<User>('/api/login', {name})
        .then(r => r.data)
}