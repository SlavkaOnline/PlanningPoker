import axios, {AxiosRequestConfig, AxiosPromise, AxiosResponse} from 'axios';
import {Session, User} from "./models";
import {createBrowserHistory} from 'history'

const history = createBrowserHistory();

axios.interceptors.request.use(request => {
    const localUser = localStorage.getItem('user');
    if (localUser) {
        const {token} = JSON.parse(localUser) as User
        request.headers.Authorization = `Bearer ${token}`;
    }
    return request;
}, error => Promise.reject(error))


axios.interceptors.response.use((response) => response,
    (error) => {
        if (error.response.status === 401) {
            history.push('/login')
        }
        return Promise.reject(error);
    });


export async function login(name: string): Promise<User> {
    return axios.post<User>('/api/login', {name})
        .then(r => r.data);
}

export async function createSession(title:string): Promise<Session> {
    return axios.post<Session>('/api/sessions', {title})
        .then(r => r.data);
}