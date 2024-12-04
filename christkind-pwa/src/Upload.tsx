import React, { useState, useRef, useEffect, ChangeEvent, FormEvent } from 'react';
import axios from 'axios';

const Upload: React.FC = () => {
    const [image, setImage] = useState<File | null>(null);
    const [description, setDescription] = useState<string>("");
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const startCamera = async () => {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ video: true });
                if (videoRef.current) {
                    videoRef.current.srcObject = stream;
                }
            } catch (error) {
                console.error('Error accessing camera:', error);
            }
        };

        startCamera();
    }, []);

    const capture = () => {
        if (canvasRef.current && videoRef.current) {
            const context = canvasRef.current.getContext('2d');
            if (context) {
                context.drawImage(videoRef.current, 0, 0, canvasRef.current.width, canvasRef.current.height);
                canvasRef.current.toBlob(blob => {
                    if (blob) {
                        const file = new File([blob], "captured_image.jpg", { type: "image/jpeg" });
                        setImage(file);
                    }
                }, 'image/jpeg');
            }
        }
    };

    const handleImageChange = (event: ChangeEvent<HTMLInputElement>) => {
        if (event.target.files) {
            setImage(event.target.files[0]);
        }
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!image) return;

        const formData = new FormData();
        formData.append('image', image);
        formData.append('description', description);

        try {
            const response = await axios.post('/api/upload', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });
            console.log(response.data);
        } catch (error) {
            console.error('Error uploading image:', error);
        }
    };

    return (
        <form onSubmit={handleSubmit} style={{textAlign:"center", display:"flex", flexDirection: "column", justifyContent:'center'}}>
           {!image ?
           <>
            <div style={{display:'flex', justifyContent:'center' }}>
                <video ref={videoRef} autoPlay width="33%"  />
                
            
            
            </div>
            
            <div>
            <button type="button" onClick={capture}>Capture Photo</button>
            <canvas ref={canvasRef} style={{ display: 'none' }} width="640" height="480"></canvas>
            
            </div>
            <div>
                <input type="file" onChange={handleImageChange} required />
            </div>
            </>:<>
            <div style={{display:'flex', justifyContent:'center' }}>
                <img src={URL.createObjectURL(image)} alt="captured" width="33%" />
                </div>
            </>}
            <div>
                <input 
                    type="text" 
                    placeholder="Add a description..." 
                    value={description} 
                    onChange={(e) => setDescription(e.target.value)} 
                />
            </div>
            <div>
                <button type="submit">Upload Drawing</button>
            </div>
        </form>
    );
};

export default Upload;