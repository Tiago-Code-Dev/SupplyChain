import { CheckCircleIcon } from '@heroicons/react/24/outline';

interface SuccessAlertProps {
  message: string;
  onDismiss?: () => void;
}

export const SuccessAlert = ({ message, onDismiss }: SuccessAlertProps) => {
  return (
    <div className="bg-green-50 border border-green-200 text-green-800 px-4 py-3 rounded-lg flex items-center justify-between">
      <div className="flex items-center">
        <CheckCircleIcon className="h-5 w-5 mr-2" />
        <span>{message}</span>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="ml-4 text-green-600 hover:text-green-800"
        >
          ×
        </button>
      )}
    </div>
  );
};


