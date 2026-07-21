import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import {
    ContractKolServiceProxy,
    ExportContractDto,
    ContractTemplateServiceProxy,
    ContractTemplateDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import moment from 'moment';

@Component({
    templateUrl: './export-contract-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        LocalizePipe,
        InputText,
        InputNumber,
        Button,
        Select,
        DatePicker,
    ],
})
export class ExportContractDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    contractKolId: string;
    exporting = false;
    templates: ContractTemplateDto[] = [];

    contractTemplateId: string;
    contractNumber: string;
    signedDate: Date = new Date();
    cccdIssueDate: Date | null = null;
    cccdIssuePlace: string;
    durationDays: number = 30;

    constructor(
        injector: Injector,
        private _contractKolService: ContractKolServiceProxy,
        private _contractTemplateService: ContractTemplateServiceProxy,
        private _http: HttpClient,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._contractTemplateService.getAll(undefined, 'Name', 0, 1000).subscribe((r) => {
            this.templates = r.items ?? [];
            this.cd.detectChanges();
        });
    }

    export(): void {
        if (!this.contractTemplateId) {
            abp.message.warn('Vui lòng chọn mẫu hợp đồng.');
            return;
        }

        this.exporting = true;
        const input = new ExportContractDto();
        input.contractKolId = this.contractKolId;
        input.contractTemplateId = this.contractTemplateId;
        input.contractNumber = this.contractNumber;
        input.signedDate = moment(this.signedDate);
        input.cccdIssueDate = this.cccdIssueDate ? moment(this.cccdIssueDate) : undefined;
        input.cccdIssuePlace = this.cccdIssuePlace;
        input.durationDays = this.durationDays;

        this._contractKolService.exportContract(input).subscribe({
            next: (result) => {
                const url = `${AppConsts.remoteServiceBaseUrl}/api/GeneratedContractFiles/${result.filePath}`;
                this._http.get(url, { responseType: 'blob' }).subscribe({
                    next: (blob) => {
                        const objectUrl = window.URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = objectUrl;
                        a.download = result.fileName ?? 'HopDong.docx';
                        a.click();
                        window.URL.revokeObjectURL(objectUrl);

                        this.exporting = false;
                        this.bsModalRef.hide();
                        this.onSave.emit();
                    },
                    error: (error) => {
                        this.exporting = false;
                        abp.message.error(this.getErrorMessage(error), 'Tải file thất bại');
                        this.cd.detectChanges();
                    },
                });
            },
            error: (error) => {
                this.exporting = false;
                abp.message.error(this.getErrorMessage(error), 'Xuất hợp đồng thất bại');
                this.cd.detectChanges();
            },
        });
    }

    // Các call POST/GET viết tay (không qua service-proxy dạng blob) không được
    // AbpHttpInterceptor tự hiển thị lỗi (interceptor chỉ xử lý khi error.error là Blob),
    // nên phải tự lấy message từ envelope lỗi chuẩn của ABP và hiển thị thủ công.
    private getErrorMessage(error: any): string {
        return error?.error?.error?.message || 'Có lỗi xảy ra, vui lòng thử lại.';
    }
}
