import axios, {CancelToken} from 'axios';
import {Session, Story, User} from "./models";
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

export async function createSession(title: string): Promise<Session> {
    return axios.post<Session>('/api/sessions', {title})
        .then(r => r.data);
}

export async function createStory(sessionId: string, title: string): Promise<Session> {
    return axios.post<Session>(`/api/sessions/${sessionId}/stories`, {title})
        .then(r => r.data);
}

export async function getSession(id: string, cancelToken?: CancelToken): Promise<Session> {
    return axios.get<Session>(`/api/sessions/${id}`, {cancelToken})
        .then(r => r.data);
}

export async function getStory(id: string, cancelToken?: CancelToken): Promise<Story> {
    return axios.get<Story>(`/api/stories/${id}`, {cancelToken})
        .then(r => r.data);
}

export async function setActiveStory(id: string, storyId: string): Promise<Session> {
    return axios.post<Session>(`/api/sessions/${id}/activestory`, {id: storyId})
        .then(r => r.data);
}

export async function vote(id: string, card: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/vote`, {card})
        .then(r => r.data);
}

export async function removeVote(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/remove_vote`)
        .then(r => r.data);
}

export async function close(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/close`)
        .then(r => r.data);
}